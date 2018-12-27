using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PeachPied.WordPress.Sdk
{
    /// <summary>
    /// Helper class providing composition host and exported components.
    /// </summary>
    [PhpHidden]
    public static class CompositionHelpers
    {
        /// <summary>
        /// Gets the composition host constructed from the assemblies in current executable folder.
        /// </summary>
        static CompositionHost CompositionHost
        {
            get
            {
                if (ReferenceEquals(_lazyhost, null))
                {
                    System.Threading.Interlocked.CompareExchange(ref _lazyhost, CreateCompositionHost(), null);
                }

                return _lazyhost;
            }
        }
        static CompositionHost _lazyhost;

        static CompositionHost CreateCompositionHost()
        {
            return new ContainerConfiguration()
                .WithAssemblies(CollectCompositionAssemblies())
                .CreateContainer();
        }

        /// <summary>Enumerates plugin providers exported from assemblies in the current executable folder.</summary>
        public static IEnumerable<IWpPluginProvider> GetProviders() => CompositionHost.GetExports<IWpPluginProvider>();

        /// <summary>
        /// Gets enumeration of plugins to be loaded into the WordPress.
        /// </summary>
        /// <param name="provider">Service provider for dependency injection.</param>
        /// <returns>Enumeration of plugin instances.</returns>
        public static IEnumerable<IWpPlugin>/*!!*/GetPlugins(IServiceProvider provider)
            => GetProviders().SelectMany(p => p.GetPlugins(provider));

        static IEnumerable<Assembly> CollectCompositionAssemblies()
        {
            var bindir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            return Directory.GetFiles(bindir, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(TryLoadAssemblyFromFile)
                .Where(x => x != null);
        }

        static Assembly TryLoadAssemblyFromFile(string fname)
        {
            try
            {
                // First quickly get the full assembly name without loading the assembly into App Domain.
                // This will be used to check whether the assembly isn't a known dependency that we don't have to load
                var name = AssemblyName.GetAssemblyName(fname);

                // LoadFrom() correctly loads the assembly while respecting shadow copying,
                // by default ASP.NET AppDomain has shadow copying enabled so all the assemblies will be loaded from cache location.
                // This avoids locking the DLLs in Bin folder.
                return IsAllowedAssembly(name) ? Assembly.LoadFrom(fname) : null;
            }
            catch
            {
                return null;
            }
        }

        static string GetPublicKeyTokenString(this AssemblyName assname)
        {
            var token = assname.GetPublicKeyToken();
            if (token != null)
            {
                return string.Join(null, token.Select(b => b.ToString("x2")));   // readable public key token, lowercased
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// It is not necessary to load (and shadow copy) all the assemblies.
        /// This method gets <c>false</c> for well known assemblies that won't be loaded.
        /// </summary>
        static bool IsAllowedAssembly(AssemblyName assname)
        {
            var tokenstr = GetPublicKeyTokenString(assname);
            if (tokenstr != null)
            {
                if (tokenstr == "5b4bee2bf1f98593" || // Peachpie
                    tokenstr == "a7d26565bac4d604" || // Google.Protobuf
                    tokenstr == "840d8b321fee7061" || // Devsense
                    tokenstr == "adb9793829ddae60" || // Microsoft
                    tokenstr == "b03f5f7f11d50a3a" || // System
                    tokenstr == "b77a5c561934e089")   // .NET
                {
                    return false;
                }
            }

            return true;
        }
    }
}
