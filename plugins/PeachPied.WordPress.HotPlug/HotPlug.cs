using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.HotPlug
{
    sealed class HotPlug : IWpPlugin
    {
        public string RootPath { get; }

        static bool WatchForChanges => true;

        readonly FolderCompiler _pluginsCompiler, _themesCompiler;

        bool _wasbuilt;

        public HotPlug(string wpRootPath, IWpPluginLogger logger)
        {
            this.RootPath = wpRootPath;

            var compiler = new CompilerProvider(RootPath);

            _pluginsCompiler = new FolderCompiler(compiler, "wp-content/plugins", "wp-plugins", logger);
            _themesCompiler = new FolderCompiler(compiler, "wp-content/themes", "wp-themes", logger);
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

            //// ajax hook to get the currently loaded assemblies version:
            //app.AddAjaxAction(
            //    "hotplug_version",
            //    () => (_pluginsCompiler.VersionLoaded.GetHashCode() ^ _themesCompiler.VersionLoaded.GetHashCode()).ToString("X"));

            //// TODO: add script to the footer that periodically checks ajax_hotplug_version for changes,
            //// in such case it refreshes the page

            // ...
        }
    }
}
