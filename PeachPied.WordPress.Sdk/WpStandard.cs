using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Sdk
{
    /// <summary>
    /// The class serves as a container for implicitly defined global PHP constants and PHP functions.
    /// See https://docs.peachpie.io/api/Libraries-Architecture/ for more details about PeachPie library architecture.
    /// </summary>
    public static class WpStandard
    {
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
        /// Overwrite how installing plugins is handled, skips the fs check.
        /// </summary>
        public const string FS_METHOD = "direct";

        // TODO: ABSPATH, WPINC, ... // frequently used constants that can be resolved statically
    }
}
