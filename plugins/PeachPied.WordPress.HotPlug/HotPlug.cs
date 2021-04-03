using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.HotPlug
{
    sealed class HotPlug : IWpPlugin
    {
        public string RootPath { get; }

        static string WpPluginsSubPath => "wp-content/plugins";

        static string WpThemesSubPath => "wp-content/themes";

        static bool WatchForChanges => true;

        readonly FolderCompiler _pluginsCompiler, _themesCompiler;

        public HotPlug(string wpRootPath, IWpPluginLogger logger)
        {
            RootPath = wpRootPath;

            var compiler = new CompilerProvider(RootPath);

            _pluginsCompiler = new FolderCompiler(compiler, WpPluginsSubPath, "wp-plugins", logger).Build(WatchForChanges);
            _themesCompiler = new FolderCompiler(compiler, WpThemesSubPath, "wp-themes", logger).Build(WatchForChanges);
        }

        void IWpPlugin.Configure(WpApp app)
        {
            // no hooks necessary
        }
    }
}
