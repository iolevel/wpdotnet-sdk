﻿using Microsoft.CodeAnalysis;
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
        #region CompilationResult

        /// <summary>
        /// The result of successful compilation.
        /// </summary>
        [DebuggerDisplay("CompilationResult ({AssemblyName,nq})")]
        sealed class CompilationResult
        {
            /// <summary>
            /// Informational assembly name.
            /// </summary>
            public string AssemblyName;

            /// <summary>
            /// Content of the compiled assembly.
            /// Can't be <c>null</c>.
            /// </summary>
            public byte[] RawAssembly;

            /// <summary>
            /// Optional. Content of the corresponding symbol file (portable PDB).
            /// </summary>
            public byte[] RawSymbols;

            /// <summary>
            /// Loads the assembly into app domain,
            /// processes contyained scripts and injects them into <see cref="Context"/>.
            /// </summary>
            public void Load()
            {
                // load in-memory assembly
                // parse its scripts and register them in Context
                Context.AddScriptReference(Assembly.Load(RawAssembly, RawSymbols));
            }
        }

        #endregion

        #region Fields & Properties

        static HashSet<string> s_ignoredErrCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PHP5011", // unreachable code
            "PHP6003", // Wrong letter case in class name
            "PHP6005", // Wrong letter case in function override
        };

        CompilerProvider Compiler { get; }

        /// <summary>
        /// Compilation helper.
        /// </summary>
        FolderCompilation _compilation = new FolderCompilation();

        /// <summary>Optional. Logger for build events.</summary>
        IWpPluginLogger Logger { get; }

        /// <summary>
        /// The root folder absolute path. Usually ".../wordpress".
        /// </summary>
        public string RootPath => Compiler.RootPath;

        /// <summary>
        /// The path relative to <see cref="RootPath"/> being watched for scripts.
        /// </summary>
        public string SubPath { get; }

        /// <summary>
        /// Full path to the folder.
        /// Normalized slashes important for the file system watcher.
        /// </summary>
        public string FullPath => Path.GetFullPath(Path.Combine(RootPath, SubPath)); // normalize slashes

        /// <summary>
        /// The in-memory assembly name.
        /// </summary>
        readonly string _assemblyNamePrefix;
        int _assemblyNameCounter;

        /// <summary>
        /// Full paths to ignored scripts.
        /// Those won't be watched and won't be compiled.
        /// </summary>
        HashSet<string> _ignoredScripts;

        /// <summary>
        /// Determines if the given file is allowed to be watched and compiled.
        /// </summary>
        bool IsAllowedFile(string fullpath)
        {
            if (fullpath.IndexOf(".phpstorm.meta.php", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullpath.IndexOf($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            // TODO: other excluded files - tests, etc.

            return _ignoredScripts == null || !_ignoredScripts.Contains(fullpath);
        }

        /// <summary>
        /// Optional. The file system watcher of <see cref="FullPath"/>.
        /// </summary>
        FileSystemWatcher _fsWatcher;

        /// <summary>
        /// Successfully built assembly, waiting to be loaded in memory lazily.
        /// </summary>
        CompilationResult _pendingBuild;

        /// <summary>
        /// Postponed action, invokes in-memory build and eventual inject into <see cref="Context"/>.
        /// </summary>
        Timer _lazyActionTimer;

        static TimeSpan s_ActionDelay => TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Flags signaling a file within <see cref="FullPath"/> has been changed
        /// and the folder needs to be rebuilt.
        /// </summary>
        bool _filesDirty;

        /// <summary>
        /// Gets an identifier signaling the currently loaded compilation "version".
        /// </summary>
        public string VersionLoaded { get; private set; }

        #endregion

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

        /// <summary>
        /// Synchronously build and load the scripts within <see cref="FullPath"/>.
        /// </summary>
        /// <param name="watch"
        /// >Whether to start watching the <see cref="FullPath"/> directory for changes.
        /// In such case, any change will result in compilation on a thread pool
        /// and eventual load into <see cref="Context"/> which effectively overrides previous compilation.
        /// </param>
        public void Build(bool watch)
        {
            DisposeWatcher();

            if (_ignoredScripts == null)
            {
                // remember scripts that were compiled in advance
                // do not compile them again nor allow them to be redefined

                Context.TryGetScriptsInDirectory(Compiler.RootPath, SubPath, out var existingscripts);

                _ignoredScripts = new HashSet<string>(
                    existingscripts.Select(s => Path.GetFullPath(Path.Combine(RootPath, s.Path))),
                    StringComparer.InvariantCultureIgnoreCase);
            }

            if (TryBuild(debug: true, out var assembly))
            {
                assembly.Load();
            }

            if (watch)
            {
                // watch the directory for changes
                // recompile the directory if necessary and
                // inject the newly compiled scripts once the compilation is successfull

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
        }

        /// <summary>
        /// Schedules <see cref="LazyAction"/>.
        /// </summary>
        void ScheduleNextAction(bool markdirty, TimeSpan delay)
        {
            if (_lazyActionTimer == null)
            {
                _lazyActionTimer = new Timer(state => LazyAction(state), this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            // Context.DeclareScript(Path.Relative(RootPath, fname), () => ... )

            _filesDirty |= markdirty;

            _lazyActionTimer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        void LazyAction(object sender)
        {
            // nothing happened for a few seconds,
            // do the dirty work now
            if (_filesDirty)
            {
                _filesDirty = false;

                // TODO: needs some versioning (critical section)

                if (TryBuild(true, out _pendingBuild))
                {
                    ScheduleNextAction(false, s_ActionDelay);
                }
            }
            else
            {
                var pending = Interlocked.Exchange(ref _pendingBuild, null);
                if (pending != null)
                {
                    pending.Load();

                    VersionLoaded = pending.AssemblyName;

                    ScheduleNextAction(false, TimeSpan.FromSeconds(60.0));
                }
                else
                {
                    // nothing happened for some time,
                    // free the cache of syntax trees
                    _compilation.InvalidateFiles();
                }
            }
        }

        void OnModified(string fname)
        {
            if (IsAllowedFile(fname))
            {
                _compilation.InvalidateFile(fname);

                ScheduleNextAction(true, s_ActionDelay);
            }
        }

        /// <summary>
        /// Postpone any pending operation a moment,
        /// the website has been just used.
        /// </summary>
        public void PostponeBuild()
        {
            if (_filesDirty || _pendingBuild != null)
            {
                ScheduleNextAction(false, s_ActionDelay);
            }
        }

        bool TryBuild(bool debug, out CompilationResult assembly)
        {
            Debug.Assert(_ignoredScripts != null);

            LogMessage($"Rebuilding '{FullPath}' ...");

            assembly = null;

            var sources = CollectSourceFiles();
            if (sources.Count == 0)
            {
                LogMessage($"No files.");
                return false;
            }

            var assname = $"{_assemblyNamePrefix}+{Interlocked.Increment(ref _assemblyNameCounter)}";

            if (_compilation.Compile(Compiler, assname, debug, sources, out var diagnostics, out var rawassembly, out var rawsymbols))
            {
                // Assembly.Load()
                assembly = new CompilationResult
                {
                    AssemblyName = assname,
                    RawAssembly = rawassembly,
                    RawSymbols = rawsymbols,
                };
            }

            Log(diagnostics);
            LogMessage($"Build finished ({(assembly != null ? "Success" : "Fail")})");

            return assembly != null;
        }

        IReadOnlyCollection<string> CollectSourceFiles()
        {
            return Directory
                .EnumerateFiles(FullPath, "*.php", SearchOption.AllDirectories)
                .Where(IsAllowedFile)
                .ToList();
        }

        void DisposeWatcher()
        {
            if (_fsWatcher != null)
            {
                _fsWatcher.Dispose();
                _fsWatcher = null;
            }
        }

        #region IDisposable

        public void Dispose()
        {
            DisposeWatcher();
        }

        #endregion
    }
}
