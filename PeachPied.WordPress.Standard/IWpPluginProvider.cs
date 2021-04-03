using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Provider of <see cref="IWpPlugin"/> instances to be loaded into the WordPress.
    /// Used for MEF export.
    /// </summary>
    public interface IWpPluginProvider
    {
        /// <summary>
        /// Gets enumeration of plugins to be loaded into the WordPress.
        /// </summary>
        /// <param name="provider">Service provider for dependency injection.</param>
        /// <param name="wpRootPath">The WordPress root path. Location of WordPress installation.</param>
        /// <returns>Enumeration of plugin instances.</returns>
        IEnumerable<IWpPlugin>/*!!*/GetPlugins(IServiceProvider provider, string wpRootPath);
    }
}
