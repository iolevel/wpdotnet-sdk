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

        public HotPlug(string wpRootPath)
        {
            RootPath = wpRootPath;

            _pluginsCompiler = new FolderCompiler(RootPath, WpPluginsSubPath, "wp-plugins").Build(WatchForChanges);
            _themesCompiler = new FolderCompiler(RootPath, WpThemesSubPath, "wp-themes").Build(WatchForChanges);
        }

        void IWpPlugin.Configure(WpApp app)
        {
            // no hooks necessary
        }
    }
}
