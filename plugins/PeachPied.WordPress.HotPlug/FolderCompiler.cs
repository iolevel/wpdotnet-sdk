using Pchp.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // TODO: ILogger

        public FolderCompiler(CompilerProvider provider, string subPath, string outputAssemblyName)
        {
            this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.SubPath = subPath ?? string.Empty;

            _assemblyName = outputAssemblyName ?? throw new ArgumentNullException(nameof(outputAssemblyName));
        }

        public FolderCompiler Build(bool watch)
        {
            // tree.Diagnostics ...

            var compilation = Provider.CreateCompilation(
                $"{_assemblyName}+{_assemblyCounter++}",
                EnumerateSourceTrees(),
                true);

            var diagnostics = compilation.GetDiagnostics();

            // ...

            return this;
        }

        IEnumerable<string> EnumerateSourceFiles()
        {
            return Directory.EnumerateFiles(FullPath, "*.php", SearchOption.AllDirectories);
        }

        IEnumerable<PhpSyntaxTree> EnumerateSourceTrees()
        {
            return EnumerateSourceFiles().Select(fname => Provider.CreateSyntaxTree(fname));
        }

        public void Dispose()
        {
            //
        }
    }
}
