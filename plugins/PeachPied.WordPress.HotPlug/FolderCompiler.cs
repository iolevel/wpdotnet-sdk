using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis;
using Pchp.Core;
using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PeachPied.WordPress.HotPlug
{
    /// <summary>
    /// Compiles scripts in the given path into in-memory assembly, and loads them.
    /// Watches for changes within the directory and re-compiles them.
    /// </summary>
    class FolderCompiler : IDisposable
    {
        [DebuggerDisplay("CompilationResult ({AssemblyName,nq})")]
        sealed class CompilationResult
        {
            public string AssemblyName;

            public byte[] RawAssembly;
            public byte[] RawSymbols;

            public void Load()
            {
                // load in-memory assembly
                // parse its scripts and register them in Context
                Context.AddScriptReference(Assembly.Load(RawAssembly, RawSymbols));
            }
        }

        static HashSet<string> s_ignoredErrCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PHP5011", // unreachable code
            "PHP6003", // Wrong letter case in class name
            "PHP6005", // Wrong letter case in function override
        };

        CompilerProvider Compiler { get; }

        IWpPluginLogger Logger { get; }

        public string RootPath => Compiler.RootPath;

        public string SubPath { get; }

        public string FullPath => Path.GetFullPath(Path.Combine(RootPath, SubPath)); // normalize slashes

        readonly string _assemblyNamePrefix;
        int _assemblyNameCounter;

        FileSystemWatcher _fsWatcher;

        /// <summary>
        /// Successfuly built assembly, waiting to be loaded in memory lazily.
        /// </summary>
        CompilationResult _pendingBuild;

        Timer _lazyAction;
        TimeSpan ActionDelay => TimeSpan.FromSeconds(2.0);

        bool _filesDirty;

        public FolderCompiler(CompilerProvider compiler, string subPath, string outputAssemblyName, IWpPluginLogger logger)
        {
            this.Compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            this.SubPath = subPath ?? string.Empty;
            this.Logger = logger;

            _assemblyNamePrefix = outputAssemblyName ?? throw new ArgumentNullException(nameof(outputAssemblyName));
        }

        void Log(DiagnosticSeverity severity, string message)
        {
            Logger?.Log(severity switch
            {
                DiagnosticSeverity.Error => IWpPluginLogger.Severity.Error,
                DiagnosticSeverity.Warning => IWpPluginLogger.Severity.Warning,
                _ => IWpPluginLogger.Severity.Message,
            }, message);
        }

        void LogMessage(string message)
        {
            Logger?.Log(IWpPluginLogger.Severity.Message, message);
        }

        void Log(IEnumerable<Diagnostic> diagnostics)
        {
            if (diagnostics != null)
            {
                foreach (var d in diagnostics)
                {
                    if (s_ignoredErrCodes.Contains(d.Id))
                    {
                        continue;
                    }

                    Log(d.Severity, $"{d.Id}: {d.GetMessage()} in {d.Location.SourceTree.FilePath}:{d.Location.GetLineSpan().StartLinePosition.Line + 1}");
                }
            }
        }

        public FolderCompiler Build(bool watch)
        {
            if (TryBuild(debug: true, out var assembly))
            {
                assembly.Load();
            }

            if (watch)
            {
                _fsWatcher = new FileSystemWatcher(FullPath, "*.php")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                };

                _fsWatcher.Created += (sender, e) => OnModified(e.FullPath);
                _fsWatcher.Deleted += (sender, e) => OnModified(e.FullPath);
                _fsWatcher.Changed += (sender, e) => OnModified(e.FullPath);
                _fsWatcher.Renamed += (sender, e) =>
                {
                    OnModified(e.OldFullPath);
                    OnModified(e.FullPath);
                };
            }

            return this;
        }

        void OnModified(string fname)
        {
            _filesDirty = true;
            Touch();
        }

        public void Touch()
        {
            if (_lazyAction == null)
            {
                Interlocked.CompareExchange(ref _lazyAction, new Timer(_ =>
                {
                    // nothing happened for a few seconds,
                    // do the dirty work now
                    if (_filesDirty)
                    {
                        _filesDirty = false;

                        if (TryBuild(true, out _pendingBuild))
                        {
                            Touch();
                        }
                    }
                    else if (_pendingBuild != null)
                    {
                        _pendingBuild.Load();
                        _pendingBuild = null;
                    }

                }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan), null);
            }

            // Context.DeclareScript(Path.Relative(RootPath, fname), () => ... )

            _lazyAction.Change(ActionDelay, Timeout.InfiniteTimeSpan);
        }

        bool TryBuild(bool debug, out CompilationResult assembly)
        {
            LogMessage($"Rebuilding '{FullPath}' ...");

            assembly = null;

            var success = true;

            var trees = ParseSourceTrees();

            if (trees.Count == 0)
            {
                LogMessage($"No files.");
                return false;
            }

            foreach (var x in trees)
            {
                success &= IsSuccess(x.Diagnostics);
            }

            if (!success)
            {
                Log(trees.SelectMany(x => x.Diagnostics));
                return false;
            }

            var assname = $"{_assemblyNamePrefix}+{Interlocked.Increment(ref _assemblyNameCounter)}";

            var compilation = Compiler.CreateCompilation(assname, trees, debug);
            var diagnostics = compilation.GetDiagnostics();

            if (IsSuccess(diagnostics))
            {
                var peStream = new MemoryStream();
                var pdbStream = debug ? new MemoryStream() : null;
                var emitOptions = new EmitOptions();

                if (debug)
                {
                    emitOptions = emitOptions.WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                }

                var result = compilation.Emit(peStream,
                    pdbStream: pdbStream,
                    options: emitOptions);

                if (result.Success)
                {
                    // Assembly.Load()
                    assembly = new CompilationResult
                    {
                        AssemblyName = assname,
                        RawAssembly = peStream.ToArray(),
                        RawSymbols = pdbStream?.ToArray(),
                    };
                }

                Log(result.Diagnostics);
            }
            else
            {
                Log(diagnostics);
            }

            LogMessage($"Build finished ({(assembly != null ? "Success" : "Fail")})");

            return assembly != null;
        }

        bool IsSuccess(ImmutableArray<Diagnostic> diagnostics)
        {
            //return diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Error)
                {
                    return false;
                }
            }

            return true;
        }

        IEnumerable<string> EnumerateSourceFiles()
        {
            return Directory.EnumerateFiles(FullPath, "*.php", SearchOption.AllDirectories);
        }

        IReadOnlyCollection<PhpSyntaxTree> ParseSourceTrees()
        {
            // TODO: cache, parse only modified

            return EnumerateSourceFiles()
                .Select(fname => Compiler.CreateSyntaxTree(fname))
                .ToList();
        }

        public void Dispose()
        {
            if (_fsWatcher != null)
            {
                _fsWatcher.Dispose();
                _fsWatcher = null;
            }
        }
    }
}
