using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    /// <summary>
    /// Provides methods of loading <see cref="WordPressConfig"/>.
    /// </summary>
    static class WpConfigurationLoader
    {
        /// <summary>
        /// Crates default configuration with default values.
        /// </summary>
        public static WordPressConfig CreateDefault()
        {
            return new WordPressConfig();
        }

        /// <summary>
        /// Loads configuration from appsettings file (<see cref="IConfiguration"/>).
        /// </summary>
        public static WordPressConfig LoadFromSettings(this WordPressConfig config, IServiceProvider services)
        {
            if (config == null)
            {
                config = CreateDefault();
            }

            var appconfig = (IConfiguration)services.GetService(typeof(IConfiguration));
            if (appconfig != null)
            {
                appconfig.GetSection("WordPress").Bind(config);
            }

            //
            return config;
        }

        /// <summary>
        /// Loads settings from a well-known environment variables.
        /// This overrides values set previously.
        /// </summary>
        public static WordPressConfig LoadFromEnvironment(this WordPressConfig config, IServiceProvider services)
        {
            if (string.IsNullOrEmpty(config.DbHost) || config.DbHost == "localhost")
            {
                // load known environment variables
                TryLoadAzureEnvVar(config);
            }

            //
            return config;
        }

        static bool HandleEnvironmentVar(string value, Func<string, string, bool> keyValueFunc)
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
                    if (eq < 0 || eq == pair.Length - 1) continue;

                    gotvalue |= keyValueFunc(pair.Remove(eq).Trim(), pair.Substring(eq + 1).Trim());
                }
            }

            return gotvalue;
        }

        static bool TryLoadAzureEnvVar(this WordPressConfig config)
        {
            return HandleEnvironmentVar(
                 Environment.GetEnvironmentVariable("MYSQLCONNSTR_localdb"),
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

                     return false;
                 });
        }
    }
}
