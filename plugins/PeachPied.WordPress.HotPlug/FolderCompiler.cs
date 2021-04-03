using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PeachPied.WordPress.HotPlug
{
    /// <summary>
    /// Compiles scripts in the given path into in-memory assembly, and loads them.
    /// Watches for changes within the directory and re-compiles them.
    /// </summary>
    class FolderCompiler : IDisposable
    {
        CompilerProvider Provider { get; }

        public string RootPath => Provider.RootPath;

        public string SubPath { get; }

        public string FullPath => Path.Combine(RootPath, SubPath);

        readonly string _assemblyName;
        int _assemblyCounter;

        public FolderCompiler(CompilerProvider provider, string subPath, string outputAssemblyName)
        {
            this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.SubPath = subPath ?? string.Empty;

            _assemblyName = outputAssemblyName ?? throw new ArgumentNullException(nameof(outputAssemblyName));
        }

        void LogInfo(string message)
        {
            // TODO: ILogger
        }

        void LogError(string message)
        {
            // TODO: ILogger
        }

        public FolderCompiler Build(bool watch)
        {
            TryBuild();

            if (watch)
            {
                // ...
            }

            return this;
        }

        bool TryBuild()
        {
            var success = true;

            var trees = ParseSourceTrees();

            foreach (var x in trees)
            {
                success &= IsSuccess(x.Diagnostics);
            }

            if (success)
            {
                var debug = true;
                var compilation = Provider.CreateCompilation(
                    $"{_assemblyName}+{_assemblyCounter++}",
                    trees,
                    debug);

                var diagnostics = compilation.GetDiagnostics();

                if (success = IsSuccess(diagnostics))
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

                    if (success = result.Success)
                    {
                        // Assembly.Load()
                    }
                }
            }

            return success;
        }

        bool IsSuccess(ImmutableArray<Diagnostic> diagnostics)
        {
            bool success = true;

            foreach (var d in diagnostics)
            {
                var message = $"{d.Id}: {d.GetMessage()} at {d.Location}";

                switch (d.Severity)
                {
                    case DiagnosticSeverity.Warning:
                        LogInfo(message);
                        break;
                    case DiagnosticSeverity.Error:
                        LogError(message);
                        success = false;
                        break;
                }
            }

            return success;
        }

        IEnumerable<string> EnumerateSourceFiles()
        {
            return Directory.EnumerateFiles(FullPath, "*.php", SearchOption.AllDirectories);
        }

        IReadOnlyCollection<PhpSyntaxTree> ParseSourceTrees()
        {
            return EnumerateSourceFiles()
                .Select(fname => Provider.CreateSyntaxTree(fname))
                .ToList();
        }

        public void Dispose()
        {
            //
        }
    }
}
