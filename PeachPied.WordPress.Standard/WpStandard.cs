using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// The class serves as a container for implicitly defined global PHP constants and PHP functions.
    /// See https://docs.peachpie.io/api/Libraries-Architecture/ for more details about PeachPie library architecture.
    /// </summary>
    public static class WpStandard
    {
        /// <summary>The name of the database for WordPress</summary>
        public static string DB_NAME { get; set; } = "wordpress";

        /// <summary>MySQL database username</summary>
        public static string DB_USER { get; set; } = "root";

        /// <summary>MySQL database password</summary>
        public static string DB_PASSWORD { get; set; } = "";

        /// <summary>MySQL hostname</summary>
        public static string DB_HOST { get; set; } = "localhost";

        /// <summary>Database Charset to use in creating database tables</summary>
        public const string DB_CHARSET = "utf8";

        /// <summary>
        /// Implicitly defined <c>WP_DEBUG</c> constant that will be used in compile time.
        /// </summary>
        public static bool WP_DEBUG { get; set; } = false;

        /// <summary></summary>
        public const bool WP_DEBUG_DISPLAY = false; // WP_DEBUG;

        /// <summary></summary>
        public const bool WP_DEBUG_LOG = false;

        /// <summary></summary>
        public static bool SCRIPT_DEBUG => WP_DEBUG;

        /// <summary></summary>
        public const bool WP_CACHE = false;

        /// <summary>
        /// Relative path to the 'wp-includes'. Cannot be changed.
        /// </summary>
        public const string WPINC = "wp-includes";

        /// <summary>
        /// Overwrite how installing plugins is handled, skips the fs check.
        /// </summary>
        public const string FS_METHOD = "direct";

        /// <summary>
        /// Disables automatic cron, we schedule it asynchronously.
        /// </summary>
        public const bool DISABLE_WP_CRON = true;

        /// <summary>
        /// Disables automatic updates as it has to be done through NuGet package update in msbuild project.
        /// </summary>
        public const bool AUTOMATIC_UPDATER_DISABLED = true;

        /// <summary>
        /// Disables automatic updates as it has to be done through NuGet package update in msbuild project.
        /// </summary>
        public const bool WP_AUTO_UPDATE_CORE = false;

        /// <summary>
        /// Autosave interval.
        /// </summary>
        public const int AUTOSAVE_INTERVAL = 60;

        // TODO: ABSPATH, ... // frequently used constants that can be resolved statically
    }
}
