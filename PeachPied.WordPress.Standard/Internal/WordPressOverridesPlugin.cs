using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
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

    sealed class WordPressOverridesPlugin : IWpPlugin
    {
        public static string WpDotNetUrl = "https://www.wpdotnet.com/";

        public static readonly string InformationalVersion = typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        bool? _registered;

        bool IsRegistered()
        {
            if (_registered.HasValue)
            {
                return _registered.Value;
            }

            return false;
        }

        void DashboardRightNow(TextWriter output)
        {
            var frameworkVersion =
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription ??
                Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            var process = System.Diagnostics.Process.GetCurrentProcess();

            string licensehtml = IsRegistered()
                ? $@"<em>registered</em>"
                : $@"<a href='#' id='wpdotnet-register' tooltip=''>Register</a> | <a href='{WpDotNetUrl}#purchase' target='_blank' tooltip=''>Purchase</a>";

            output.Write($@"
<table style=""border:0;width:100%;""><tbody>
<tr>
    <td><img src=""https://github.com/iolevel/wpdotnet-sdk/raw/master/wpdotnet.png"" style=""width:76px;margin:8px;display:inline;""></td>
    <td>
        <b>Your WordPress runs on {frameworkVersion}</b>
        <div>
            <b title=""Memory allocated by the whole process."">Memory usage:</b> {process.WorkingSet64 / 1024 / 1024} MB<br/>
            <b title=""CPU time consumed by the whole process."">CPU usage:</b> {process.TotalProcessorTime:c}<br/>
            <b>License:</b> {licensehtml}<br/>
        </div>
    </td>
</tr>
</tbody></table>");
        }

        static readonly string GeneratorHtml = $"<meta name=\"generator\" content=\"WpDotNet (PeachPie) {InformationalVersion} \" />";

        public void Configure(WpApp app)
        {
            //
            // Dashboard:
            //

            app.OnAdminInit(() =>
            {

            });

            // add information into Right Now section
            app.DashboardRightNow(DashboardRightNow);

            //
            // Blogs:
            //

            // alter generator metadata
            app.AddFilter("get_the_generator_xhtml", new Func<string>(() => GeneratorHtml));

            //             app.AddFilter("wp_head", new Action(() =>
            //             {
            //                 app.Context.Output.Write(@"<style>
            // .foldtl {
            //   position: relative;
            //   -webkit-box-shadow: 5px -5px 5px rgba(0,0,0,0.2);
            //   -moz-box-shadow: 5px -5px 5px rgba(0,0,0,0.2);
            //   box-shadow: 5px -5px 5px rgba(0,0,0,0.2);
            // }
            // .foldtl:before {
            //   content: """";
            //   position: absolute;
            //   top: 0%;
            //   left: 0%;
            //   width: 0px;
            //   height: 0px;
            //   border-top: 70px solid rgba(255,255,255,0.8);
            //   border-left: 70px solid #ededed;
            //   -webkit-box-shadow: 7px -7px 7px rgba(0,0,0,0.2);
            //   -moz-box-shadow: 7px -7px 7px rgba(0,0,0,0.2);
            //   box-shadow: 7px -7px 7px rgba(0,0,0,0.2);
            // }
            // </style>");
            //             }));

            if (!IsRegistered())
            {
                app.Footer(output =>
                {
                    output.Write(@$"
<div style='position:fixed;text-align:center; width:128px; height:50px; bottom:0; right:0;padding:12px 0 12px 0; margin:0 auto; vertical-align: middle;background:#68b227;border-left:solid 6px rgba(255,255,255,.5);line-height:24px;'>
    <a href='{WpDotNetUrl}?from=footer#pricing' style='color:#fff;font-size:16px;font-style:tahoma,helvetica;padding:0; margin:0;'>Get Rid of Me</a>
</div>
");
                });
            }
        }
    }
}
