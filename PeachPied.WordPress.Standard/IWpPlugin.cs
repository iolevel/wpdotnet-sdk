using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Interface representing a WordPress plugin.
    /// </summary>
    public interface IWpPlugin
    {
        /// <summary>
        /// Initializes request to WordPress site.
        /// </summary>
        void Configure(WpApp app);
    }
}
