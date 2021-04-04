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

        bool _wasbuilt;

        public HotPlug(string wpRootPath, IWpPluginLogger logger)
        {
            this.RootPath = wpRootPath;

            var compiler = new CompilerProvider(RootPath);

            _pluginsCompiler = new FolderCompiler(compiler, WpPluginsSubPath, "wp-plugins", logger);
            _themesCompiler = new FolderCompiler(compiler, WpThemesSubPath, "wp-themes", logger);
        }

        /// <summary>
        /// In case of the first request,
        /// this invokes the compiler to build user's plugins and themes for the first time
        /// and initializes the file sytem watchers.
        /// </summary>
        void FirstRequest()
        {
            if (_wasbuilt)
            {
                return;
            }

            //

            lock (this)
            {
                if (!_wasbuilt)
                {
                    // compile plugins and themes on first use
                    _pluginsCompiler.Build(WatchForChanges);
                    _themesCompiler.Build(WatchForChanges);

                    //
                    _wasbuilt = true;
                }
            }
        }

        void IWpPlugin.Configure(WpApp app)
        {
            FirstRequest();

            // delay the build action,
            // website is being just requested
            _pluginsCompiler.PostponeBuild();
            _themesCompiler.PostponeBuild();

            // wp hooks:
            // ...
        }
    }
}
