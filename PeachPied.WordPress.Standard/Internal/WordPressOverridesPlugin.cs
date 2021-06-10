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
        public static string WpDotNetUrl = "https://github.com/iolevel/wpdotnet-sdk";

        public static readonly string InformationalVersion = typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        static void DashboardRightNow(TextWriter output)
        {
            var frameworkVersion =
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription ??
                Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            var process = System.Diagnostics.Process.GetCurrentProcess();

            output.Write($@"
<table style=""border:0;width:100%;""><tbody>
<tr>
    <td><img src=""https://github.com/iolevel/wpdotnet-sdk/raw/master/wpdotnet.png"" style=""width:76px;margin:8px;display:inline;""></td>
    <td>
        <b>Your WordPress runs on {frameworkVersion}</b>
        <div>
            <b title=""Memory allocated by the whole process."">Memory usage:</b> {process.WorkingSet64 / 1024 / 1024} MB<br/>
            <b title=""CPU time consumed by the whole process."">CPU usage:</b> {process.TotalProcessorTime:c}<br/>
            <b>Project URL:</b> <a href=""{WpDotNetUrl}"" target=""_blank"">wpdotnet-sdk</a><br/>
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

            // add information into Right Now section
            app.DashboardRightNow(DashboardRightNow);

            //
            // Blogs:
            //

            // alter generator metadata
            app.AddFilter("get_the_generator_xhtml", new Func<string>(() => GeneratorHtml));

            app.Footer(output =>
            {
                output.Write(@"
<div style='position:fixed;text-align:center; width:128px; height:50px; bottom:0; right:0;padding:12px 0 12px 0; margin:0 auto; vertical-align: middle;background:#68b227;border-left:solid 6px rgba(255,255,255,.5);line-height:24px;'>
    <a href='https://www.wpdotnet.com/#pricing' style='color:#fff;font-size:16px;font-style:tahoma,helvetica;padding:0; margin:0;'>Get Rid of Me</a>
</div>
");
                //output.Write(@"<div style='margin:-55px 0 0 0;width:70px;height:70px;left:0;' class='foldtl'>&nbsp;</div>");
            });
        }
    }
}
