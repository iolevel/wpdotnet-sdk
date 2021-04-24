using Microsoft.CodeAnalysis;
using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

            app.AddFilter("admin_init", new Action(() =>
            {
                // render notices if there are compile time errors
                app.AdminNotices(() => CollectAdminNotices(app));
            }));

            //// ajax hook to get the currently loaded assemblies version:
            //app.AddAjaxAction(
            //    "hotplug_version",
            //    () => (_pluginsCompiler.VersionLoaded.GetHashCode() ^ _themesCompiler.VersionLoaded.GetHashCode()).ToString("X"));

            //// TODO: add script to the footer that periodically checks ajax_hotplug_version for changes,
            //// in such case it refreshes the page

            // ...
        }

        string IconHtml(Diagnostic d)
        {
            return null;
        }

        string LocationHtml(Location location)
        {
            if (location.SourceTree != null)
            {
                var pos = location.GetLineSpan().StartLinePosition;
                return $"{location.SourceTree.FilePath.Replace(RootPath, "", StringComparison.InvariantCultureIgnoreCase)} ({pos.Line + 1},{pos.Character + 1})";
            }

            return null;
        }

        string CollectAdminNotices(WpApp app, ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefaultOrEmpty)
            {
                return null;
            }

            var rows = new List<string>(diagnostics.Length);

            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Hidden)
                {
                    continue;
                }

                rows.Add($@"
<tr>
    <td>{IconHtml(d)}</td>
    <td style='font-weight:500;'>{d.Id}</td>
    <td>{LocationHtml(d.Location)}:</td>
    <td>{d.GetMessage()}</td>
</tr>
");

                var noticeclass = d.Severity == DiagnosticSeverity.Error ? "notice-error" : "notice-warning";

                //result.Append(@$"<div class=""notice {noticeclass} is-dismissible""><p>");
                //result.Append(d.ToString().Replace(RootPath, "", StringComparison.InvariantCultureIgnoreCase));
                //result.Append(@$"</p></div>");
            }

            return @$"<div class='notice notice-warning is-dismissible' style='max-height:10.5rem;overflow-y:scroll;'>"
                + "<table>"
                + string.Join("", rows)
                + "</table></div>";
        }

        string CollectAdminNotices(WpApp app)
        {
            var pagenow = app.Context.Globals["pagenow"].ToString();
            return pagenow switch
            {
                "plugins.php" => CollectAdminNotices(app, _pluginsCompiler.LastDiagnostics),
                "themes.php" => CollectAdminNotices(app, _themesCompiler.LastDiagnostics),
                _ => null,
            };
        }
    }
}
