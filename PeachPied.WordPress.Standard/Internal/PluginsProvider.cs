using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;

namespace PeachPied.WordPress.Standard.Internal
{
    [Export(typeof(IWpPluginProvider))]
    sealed class PluginsProvider : IWpPluginProvider
    {
        public IEnumerable<IWpPlugin> GetPlugins(IServiceProvider provider, string wpRootPath)
        {
            yield return new WordPressOverridesPlugin();
        }
    }
}
