using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace PeachPied.WordPress.HotPlug
{
    class FolderCompilation
    {
        readonly ConcurrentDictionary<string, PhpSyntaxTree> _parsedtrees = new ConcurrentDictionary<string, PhpSyntaxTree>(StringComparer.InvariantCultureIgnoreCase);

        public void InvalidateFile(string filename) => _parsedtrees.TryRemove(filename, out _);

        public void InvalidateFiles() => _parsedtrees.Clear();

        public PhpSyntaxTree GetOrAddFile(string filename) => _parsedtrees.GetOrAdd(filename, _fname =>
        {
            using var fstream = File.OpenRead(filename);

            return PhpSyntaxTree.ParseCode(
                SourceText.From(fstream),
                new PhpParseOptions(
                    kind: SourceCodeKind.Regular,
                    //languageVersion: CoreCompilation.Options.LanguageVersion,
                    shortOpenTags: false),
                PhpParseOptions.Default,
                filename
            );
        });

        static bool IsSuccess(ImmutableArray<Diagnostic> diagnostics)
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

        public bool Compile(
            CompilerProvider compiler, string assname, bool debug,
            IReadOnlyCollection<string> files,
            out ImmutableArray<Diagnostic> diagnostics,
            out byte[] rawassembly, out byte[] rawsymbols)
        {
            rawassembly = null;
            rawsymbols = null;

            var success = true;
            var trees = files.Select(f => GetOrAddFile(f)).ToList();

            foreach (var x in trees)
            {
                success &= IsSuccess(x.Diagnostics);
            }

            if (!success)
            {
                diagnostics = trees.SelectMany(x => x.Diagnostics).ToImmutableArray();
                return false;
            }

            var compilation = compiler.CreateCompilation(assname, trees, debug);

            var analysis = compilation.GetDiagnostics();

            if (IsSuccess(analysis))
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

                diagnostics = result.Diagnostics;

                if (result.Success)
                {
                    rawassembly = peStream.ToArray();
                    rawsymbols = pdbStream?.ToArray();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                diagnostics = analysis;
                return false;
            }

        }
    }
}
