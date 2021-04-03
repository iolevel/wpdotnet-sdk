using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Helper class providing composition host and exported components.
    /// </summary>
    [PhpHidden]
    public static class CompositionHelpers
    {
        /// <summary>Enumerates plugin providers exported from assemblies in the current executable folder.</summary>
        public static IEnumerable<IWpPluginProvider> GetProviders(CompositionHost host) => host.GetExports<IWpPluginProvider>();

        /// <summary>
        /// Gets enumeration of plugins to be loaded into the WordPress.
        /// </summary>
        /// <param name="host">Instance of <see cref="CompositionHost"/> providing exported parts.</param>
        /// <param name="provider">Service provider for dependency injection.</param>
        /// <param name="wpRootPath">The WordPress root path. Location of WordPress installation.</param>
        /// <returns>Enumeration of plugin instances.</returns>
        public static IEnumerable<IWpPlugin>/*!!*/GetPlugins(CompositionHost host, IServiceProvider provider, string wpRootPath)
        {
            // import providers from composition host:
            IEnumerable<IWpPluginProvider> providers = host != null
                ? GetProviders(host)
                : Enumerable.Empty<IWpPluginProvider>();

            // hardcode our internal plugin:
            providers = new[] { new Internal.PluginsProvider() }.Concat(providers);

            // create plugins:
            return providers.SelectMany(p => p.GetPlugins(provider, wpRootPath));
        }

        //static IEnumerable<Assembly> CollectCompositionAssemblies()
        //{
        //    // {app} itself
        //    yield return Assembly.GetEntryAssembly();

        //    // PeachPied.WordPress.AspNetCore
        //    if (TryLoadAssembly("PeachPied.WordPress.AspNetCore", out var ass))
        //    {
        //        yield return ass;
        //    }

        //    // PeachPied.WordPress.AspNetCore
        //    if (TryLoadAssembly("PeachPied.WordPress.DotNetBridge", out ass))
        //    {
        //        yield return ass;
        //    }

        //    // TODO: config with assembly names?
        //}

        static bool TryLoadAssembly(string name, out Assembly ass)
        {
            try
            {
                ass = Assembly.Load(new AssemblyName(name));
            }
            catch
            {
                ass = null;
            }

            return ass != null;
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
