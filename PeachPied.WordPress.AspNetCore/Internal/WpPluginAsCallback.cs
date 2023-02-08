using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal sealed class WpPluginAsCallback : IWpPlugin
    {
        Action<WpApp> Configure { get; }

        public WpPluginAsCallback(Action<WpApp> configure)
        {
            Configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        ValueTask IWpPlugin.ConfigureAsync(WpApp app, CancellationToken token)
        {
            Configure(app);
            return ValueTask.CompletedTask;
        }
    }
}
