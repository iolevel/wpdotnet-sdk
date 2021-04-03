﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PeachPied.WordPress.HotPlug
{
    sealed class CompilerProvider
    {
        public string RootPath { get; }

        PhpCompilation _compilation;
        IAssemblySymbol _assemblytmp;

        public CompilerProvider(string rootPath)
        {
            this.RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        }

        PhpCompilation CoreCompilation
        {
            get
            {
                if (_compilation == null)
                {
                    if (Interlocked.CompareExchange(ref _compilation, CreateDefaultCompilation(), null) == null)
                    {
                        // bind reference manager, cache all references
                        _assemblytmp = _compilation.Assembly;
                    }
                }

                //
                return _compilation;
            }
        }

        PhpCompilation CreateDefaultCompilation()
        {
            return PhpCompilation.Create("project",
                references: MetadataReferences().Select(CreateMetadataReference),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: RootPath,
                    sdkDirectory: null));
        }

        public PhpCompilation CreateCompilation(string assemblyName, IEnumerable<PhpSyntaxTree> sources, bool debug)
        {
            var compilation = (PhpCompilation)CoreCompilation
                    //.WithLangVersion(languageVersion)
                    .WithAssemblyName(assemblyName)
                    .AddSyntaxTrees(sources)
                    //.AddReferences(metadatareferences)
                    ;

            if (debug)
            {
                compilation = compilation.WithPhpOptions(compilation.Options.WithOptimizationLevel(OptimizationLevel.Debug).WithDebugPlusMode(true));
            }
            else
            {
                compilation = compilation.WithPhpOptions(compilation.Options.WithOptimizationLevel(OptimizationLevel.Release));
            }

            return compilation;
        }

        public PhpSyntaxTree CreateSyntaxTree(string filename)
        {
            using var fstream = File.OpenRead(filename);

            return PhpSyntaxTree.ParseCode(
                SourceText.From(fstream),
                new PhpParseOptions(
                    kind: SourceCodeKind.Regular,
                    languageVersion: CoreCompilation.Options.LanguageVersion,
                    shortOpenTags: false),
                PhpParseOptions.Default,
                filename
            );
        }

        static MetadataReference CreateMetadataReference(string path) => MetadataReference.CreateFromFile(path);

        /// <summary>
        /// Collect references we have to pass to the compilation.
        /// </summary>
        static IEnumerable<string> MetadataReferences()
        {
            // implicit references
            var impl = new List<Assembly>(8)
            {
                typeof(object).Assembly,                 // mscorlib (or System.Runtime)
                typeof(Pchp.Core.Context).Assembly,      // Peachpie.Runtime
                typeof(Pchp.Library.Strings).Assembly,   // Peachpie.Library
                typeof(Peachpie.Library.Graphics.PhpGd2).Assembly,
                typeof(Peachpie.Library.MySql.MySql).Assembly,
                typeof(Peachpie.Library.Network.CURLFunctions).Assembly,
                typeof(Peachpie.Library.XmlDom.XmlDom).Assembly,
                typeof(WP).Assembly,                     // WordPress and its dependencies
            };

            var set = new HashSet<Assembly>();

            set.UnionWith(impl);
            set.UnionWith(Pchp.Core.Context.GetScriptReferences());  // PHP assemblies, excluding eval'ed code

            var todo = new List<Assembly>(set);

            for (int i = 0; i < todo.Count; i++)
            {
                foreach (var refname in todo[i].GetReferencedAssemblies())
                {
                    Assembly refassembly;

                    try
                    {
                        refassembly = Assembly.Load(refname);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    if (refassembly != null && set.Add(refassembly))
                    {
                        todo.Add(refassembly);
                    }
                }
            }

            return set.Select(ass => ass.Location);
        }
    }
}
