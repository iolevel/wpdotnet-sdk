using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace PeachPied.WordPress.Standard.Internal
{
    [Export(typeof(IWpPluginProvider))]
    sealed class PluginsProvider : IWpPluginProvider
    {
        public IEnumerable<IWpPlugin> GetPlugins(IServiceProvider provider)
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
    <td><img src=""https://raw.githubusercontent.com/peachpiecompiler/peachpie/master/docs/logos/round-orange-196x196.png"" style=""width:76px;margin:8px;display:inline;""></td>
    <td>
        <b>Your WordPress is running on .NET</b>
        <div>
            <b>Framework:</b> {frameworkVersion}<br/>
            <b title=""Memory allocated by the whole process."">Memory usage:</b> {process.WorkingSet64/1024/1024} MB<br/>
            <b title=""CPU time consumed by the whole process."">CPU usage:</b> {process.TotalProcessorTime:c}<br/>
            <b>Project URL:</b> <a href=""{WpDotNetUrl}"" target=""_blank"">wpdotnet-sdk</a><br/>
        </div>
    </td>
</tr>
</tbody></table>");
        }

        static readonly string GeneratorHtml = "<meta name=\"generator\" content=\"WpDotNet (PeachPie) " + InformationalVersion + " \" />";

        public void Configure(WpApp app)
        {
            //
            // Dashboard:
            //

            // add information into Right Now section
            app.DashboardRightNow(DashboardRightNow);

            // do not allow editing of .php files:
            app.AddFilter("editable_extensions", new Func<IList, IList>(editable_extensions =>
            {
                editable_extensions.Remove("php");
                return editable_extensions;
            }));

            //
            // Blogs:
            //

            // alter generator metadata
            app.AddFilter("get_the_generator_xhtml", new Func<string>(() => GeneratorHtml));
        }
    }
}
