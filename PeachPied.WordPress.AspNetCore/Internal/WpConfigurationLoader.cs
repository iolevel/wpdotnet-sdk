using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyModel;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    /// <summary>
    /// Provides methods of loading <see cref="WordPressConfig"/>.
    /// </summary>
    static class WpConfigurationLoader
    {
        /// <summary>
        /// Adds implicit configuration values.
        /// Loads containers from dependency context.
        /// </summary>
        public static WordPressConfig LoadDefaults(this WordPressConfig config)
        {
            var PeachPiedWordPress = typeof(WP).Assembly.GetName().Name; // "PeachPied.WordPress"
            var WordPressStandard = typeof(Standard.WpStandard).Assembly.GetName().Name; // "PeachPied.WordPress.Standard"

            // reads dependencies from app's DependencyContext
            foreach (var lib in DependencyContext.Default.RuntimeLibraries)
            {
                if (lib.Type != "package" && lib.Type != "project")
                {
                    continue;
                }

                bool HasPeachPiedWordPress = false;
                bool HasWordPressStandard = false;

                for (int dep = 0; dep < lib.Dependencies.Count; dep++)
                {
                    var depname = lib.Dependencies[dep].Name;
                    HasPeachPiedWordPress |= depname == PeachPiedWordPress;
                    HasWordPressStandard |= depname == WordPressStandard;
                }

                if (HasWordPressStandard || HasPeachPiedWordPress)
                {
                    var ass = Assembly.Load(new AssemblyName(lib.Name));
                    if (ass.GetType(Pchp.Core.Context.ScriptInfo.ScriptTypeName) != null)
                    {
                        // PHP assembly
                        //(config.LegacyPluginAssemblies ??= new List<string>()).Add(ass.FullName);
                        Pchp.Core.Context.AddScriptReference(ass);
                    }
                    else
                    {
                        // not PHP assembly
                        // add as MEF composition container
                        config.CompositionContainers.WithAssembly(ass);
                    }
                }
            }

            //config.CompositionContainers
            //    //.WithAssembly(Assembly.GetEntryAssembly()) // {app} itself
            //    //.WithAssembly(typeof(Provider).Assembly)
            //    .WithAssembly(Assembly.Load("PeachPied.WordPress.NuGetPlugins"))
            //    ;

            //
            return config;
        }

        /// <summary>
        /// Loads configuration from appsettings file (<see cref="IConfiguration"/>).
        /// </summary>
        public static WordPressConfig LoadFromSettings(this WordPressConfig config, IServiceProvider services)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (services.TryGetService<IConfiguration>(out var appconfig))
            {
                appconfig.GetSection("WordPress").Bind(config);
            }

            //
            return config;
        }

        /// <summary>
        /// Loads configuration from <see cref="IConfigureOptions{WordPressConfig}"/> service.
        /// </summary>
        public static WordPressConfig LoadFromOptions(this WordPressConfig config, IServiceProvider services)
        {
            if (services.TryGetService<IConfigureOptions<WordPressConfig>>(out var configservice))
            {
                configservice.Configure(config);
            }

            return config;
        }
        /// <summary>
        /// Loads settings from well-known environment variables.
        /// This might override values set previously.
        /// </summary>
        public static WordPressConfig LoadFromEnvironment(this WordPressConfig config, IServiceProvider services)
        {
            if (string.IsNullOrEmpty(config.DbHost) || config.DbHost == "localhost")
            {
                // load known environment variables
                TryLoadAzureEnvVar(config, Environment.GetEnvironmentVariable("MYSQLCONNSTR_localdb"));
            }

            //
            return config;
        }

        static bool ParseEnvironmentVar(string value, Func<string, string, bool> keyValueFunc)
        {
            // parses the environment variable separated with semicolon
            // Template: NAME=VALUE;NAME=VALUE;...
            // not expecting ';' or quotes in the value

            bool gotvalue = false;

            if (!string.IsNullOrEmpty(value))
            {
                foreach (var pair in value.Split(';'))
                {
                    var eq = pair.IndexOf('=');
                    if (eq > 0 && eq < pair.Length)
                    {
                        gotvalue |= keyValueFunc(
                            pair.AsSpan(0, eq).Trim().ToString(),
                            pair.AsSpan(eq + 1).Trim().ToString());
                    }
                }
            }

            return gotvalue;
        }

        static bool TryLoadAzureEnvVar(this WordPressConfig config, string connectionString)
        {
            return ParseEnvironmentVar(
                 connectionString,
                 (name, value) =>
                 {
                     if (name.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                     {
                         config.DbHost = value;
                         return true;
                     }

                     if (name.Equals("User Id", StringComparison.OrdinalIgnoreCase))
                     {
                         config.DbUser = value;
                         return true;
                     }

                     if (name.Equals("Password", StringComparison.OrdinalIgnoreCase))
                     {
                         config.DbPassword = value;
                         return true;
                     }

                     if (name.Equals("Database", StringComparison.OrdinalIgnoreCase))
                     {
                         config.DbName = value;
                         return true;
                     }

                     return false;
                 });
        }
    }
}
