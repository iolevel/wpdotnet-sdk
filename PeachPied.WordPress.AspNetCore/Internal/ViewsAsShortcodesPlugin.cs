using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal sealed class ViewsAsShortcodesPlugin : IWpPlugin, IWpPluginProvider // TODO
    {
        readonly IViewComponentDescriptorCollectionProvider _viewsProvider;

        public ViewsAsShortcodesPlugin(IViewComponentDescriptorCollectionProvider viewsProvider)
        {
            _viewsProvider = viewsProvider;
        }

        ValueTask IWpPlugin.ConfigureAsync(WpApp app, CancellationToken token)
        {
            if (_viewsProvider != null)
            {
                foreach (var item in _viewsProvider.ViewComponents.Items)
                {

                }
            }

            return ValueTask.CompletedTask;
        }

        IEnumerable<IWpPlugin> IWpPluginProvider.GetPlugins(IServiceProvider provider, string wpRootPath) => new[] { this };
    }
}
