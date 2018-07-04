using System.Collections.Generic;
using PeachPied.WordPress.Sdk;

namespace PeachPied.WordPress.AspNetCore
{
    /// <summary>
    /// WordPress configuration.
    /// The configuration is loaded into WordPress before every request.
    /// </summary>
    public class WordPressConfig
    {
        /// <summary>MySQL database name.</summary>
        public string DbName { get; set; } = "wordpress";

        /// <summary>MySQL database user name.</summary>
        public string DbUser { get; set; } = "root";

        /// <summary>MySQL database password.</summary>
        public string DbPassword { get; set; }

        /// <summary>MySQL host.</summary>
        public string DbHost { get; set; } = "localhost";

        /// <summary>
        /// Enumeration of WordPress plugins to be loaded.
        /// </summary>
        public IEnumerable<IWpPlugin> Plugins { get; set; }

        /// <summary>
        /// Whether to enable or disable response caching.
        /// Enabled by default.
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// Enumeration of assembly names with compiled PHP plugins, themes or other additions.
        /// </summary>
        public IEnumerable<string> LegacyPluginAssemblies { get; set; }
    }
}
