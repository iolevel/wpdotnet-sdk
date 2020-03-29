using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Object used to be called from WordPress code,
    /// instantiated into a global PHP variable <c>$peachpie_wp_loader</c>.
    /// </summary>
    public class WpLoader
    {
        /// <summary>
        /// Gets collection of WordPress plugins to be used.
        /// </summary>
        readonly List<IWpPlugin> _plugins = new List<IWpPlugin>();

        /// <summary>
        /// Initializes the loader object.
        /// </summary>
        public WpLoader(IEnumerable<IWpPlugin> plugins)
        {
            if (plugins != null)
            {
                _plugins.AddRange(plugins);
            }
        }

        /// <summary>
        /// Invoked by PHP plugin implementation (peachpie-api.php) to bridge into .NET.
        /// </summary>
        public virtual void AppStarted(WpApp app)
        {
            // activate plugins:
            foreach (var plugin in _plugins)
            {
                plugin.Configure(app);
            }
        }
    }
}
