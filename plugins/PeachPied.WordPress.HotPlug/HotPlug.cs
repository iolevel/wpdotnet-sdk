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

            app.OnAdminInit(() =>
            {
                // render notices if there are compile time errors
                app.AdminNotices(() => CollectAdminNotices(app));
            });

            app.AdminMenu(() =>
            {
                var hook = app.AddManagementPage("Code Problems", "Code Problems", "install_plugins", "list-problems", (output) =>
                {
                    var hasany = _pluginsCompiler.LastDiagnostics.Length != 0 || _themesCompiler.LastDiagnostics.Length != 0;
                    var maxseverity = hasany
                        ? _pluginsCompiler.LastDiagnostics.Concat(_themesCompiler.LastDiagnostics).Max(d => d.Severity)
                        : DiagnosticSeverity.Hidden;

                    var overallicon = hasany ? IconHtml(maxseverity) : IconHtmlSuccess();

                    output.Write($@"
<div style='margin:16px;'>
	<div><h1>Code Problems</h1></div>
	<div class='hide-if-no-js orange'>
		<div>{overallicon} {(maxseverity != 0 ? "Should be checked." : "No problems.")}</div>
	</div>
</div>");
                    if (_pluginsCompiler.LastDiagnostics.Length != 0 || _themesCompiler.LastDiagnostics.Length != 0)
                    {
                        output.Write("<div style='margin:24px;padding:8px;background:white;border:solid 1px #aaa;'>");

                        output.Write(CreateDiagnosticsTable(_pluginsCompiler.LastDiagnostics, true));

                        output.Write(CreateDiagnosticsTable(_themesCompiler.LastDiagnostics, true));

                        output.Write("</div>");
                    }

                }, 4);
                //app.AddFilter($"load-{hook}", new Action(() =>
                //{
                //    //
                //}));
            });

            app.Footer(output =>
            {
//                 output.Write(@$"
// <div style='margin:4px auto;padding:6px;text-align:right;background:#eee;border-top:solid 1px #ccc;color:#444;font-size:13px;'>
// Built with WordPress on .NET
// </div>");
            });

            //// ajax hook to get the currently loaded assemblies version:
            //app.AddAjaxAction(
            //    "hotplug_version",
            //    () => (_pluginsCompiler.VersionLoaded.GetHashCode() ^ _themesCompiler.VersionLoaded.GetHashCode()).ToString("X"));

            //// TODO: add script to the footer that periodically checks ajax_hotplug_version for changes,
            //// in such case it refreshes the page

            // ...
        }

        static string IconHtml(Diagnostic d) => IconHtml(d.Severity);

        static string IconHtml(DiagnosticSeverity severity) => severity switch
        {
            DiagnosticSeverity.Warning => IconHtml(Resources.notification_warn),
            DiagnosticSeverity.Error => IconHtml(Resources.notification_error),
            _ => IconHtml(Resources.notification_info),
        };

        static string IconHtmlSuccess() => IconHtml(Resources.notification_success);

        static string IconHtml(byte[] pngbytes)
        {
            if (pngbytes == null)
            {
                return null;
            }

            return $"<img style='width:1rem;height:1rem;user-select:none;' src='data:image/png;base64,{System.Web.HttpUtility.HtmlAttributeEncode(Convert.ToBase64String(pngbytes))}' />";
        }

        string LocationHtml(Location location)
        {
            const string pluginsprefix = "/wp-content/plugins/";

            if (location.SourceTree != null)
            {
                var pos = location.GetLineSpan().StartLinePosition;
                var path = location.SourceTree.FilePath
                    .Replace(RootPath, "", StringComparison.InvariantCultureIgnoreCase)
                    .Replace('\\', '/'); // plugin-editor expects forward slashes always

                var pathhtml = path;

                if (path.StartsWith(pluginsprefix))
                {
                    var file = System.Web.HttpUtility.UrlEncode(path.Substring(pluginsprefix.Length));
                    pathhtml = $"<a href='/wp-admin/plugin-editor.php?file={file}'>{path}</a>";
                }

                return $"{pathhtml} ({pos.Line + 1},{pos.Character + 1})";
            }

            return null;
        }

        string CreateDiagnosticsTable(ImmutableArray<Diagnostic> diagnostics, bool showhidden = false)
        {
            if (diagnostics.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            var rows = new List<string>(diagnostics.Length);

            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Hidden && !showhidden)
                {
                    continue;
                }

                var trstyle = (rows.Count % 2) == 1
                    ? "background-color:#eee;"
                    : "";

                rows.Add($@"
<tr style='padding-left:8px;{trstyle}'>
    <td>{IconHtml(d)}</td>
    <td style='font-weight:500;'><a href='https://docs.peachpie.io/php/diagnostics/' target='_blank'>{d.Id}</a></td>
    <td>{LocationHtml(d.Location)}:</td>
    <td>{d.GetMessage()}</td>
</tr>
");

                var noticeclass = d.Severity == DiagnosticSeverity.Error ? "notice-error" : "notice-warning";

                //result.Append(@$"<div class=""notice {noticeclass} is-dismissible""><p>");
                //result.Append(d.ToString().Replace(RootPath, "", StringComparison.InvariantCultureIgnoreCase));
                //result.Append(@$"</p></div>");
            }

            return @$"<table style='padding:8px;width:100%;'>" + string.Join("", rows) + "</table>";
        }

        string CollectAdminNotices(ImmutableArray<Diagnostic> diagnostics)
        {
            var tablehtml = CreateDiagnosticsTable(diagnostics);
            if (string.IsNullOrEmpty(tablehtml))
            {
                return null;
            }

            return @$"<div class='notice notice-warning is-dismissible' style='max-height:10.5rem;overflow-y:scroll;'>"
                + tablehtml
                + "</div>";
        }

        string CollectAdminNotices(WpApp app)
        {
            var pagenow = app.Context.Globals["pagenow"].ToString();
            return pagenow switch
            {
                "plugins.php" => CollectAdminNotices(_pluginsCompiler.LastDiagnostics),
                "themes.php" => CollectAdminNotices(_themesCompiler.LastDiagnostics),
                _ => null,
            };
        }
    }
}
