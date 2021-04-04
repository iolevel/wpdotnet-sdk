using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using Pchp.Core;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.DotNet
{
    [Export(typeof(IWpPluginProvider))]
    sealed class Provider : IWpPluginProvider
    {
        IEnumerable<IWpPlugin>/*!!*/IWpPluginProvider.GetPlugins(IServiceProvider provider, string wpRootPath)
        {
            yield return new DotNetOverridesPlugin();
        }
    }
}