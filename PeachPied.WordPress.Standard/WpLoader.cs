using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;

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

        async ValueTask AppStartedAsync(WpApp app, CancellationToken token = default)
        {
            // activate plugins:
            foreach (var plugin in _plugins)
            {
                await plugin.ConfigureAsync(app, token);
            }
        }

        /// <summary>
        /// Invoked by PHP plugin implementation (DotNetBridge.php) to pass the WpApp PHP class.
        /// </summary>
        public static void AppStarted(Context ctx, WpApp host)
        {
            if (ctx.Globals["peachpie_wp_loader"].AsObject() is WpLoader loader)
            {
                loader
                    .AppStartedAsync(host)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
