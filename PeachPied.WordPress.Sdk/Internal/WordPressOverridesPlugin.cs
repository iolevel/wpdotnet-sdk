using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Reflection;
using System.Text;

namespace PeachPied.WordPress.Sdk.Internal
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
        public static readonly string InformationalVersion = typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        static void DashboardRightNow(TextWriter output)
        {
            output.Write(
                "<ul>" +
                "<li><img src='{0}' style='width:76px;margin:0 auto;display:block;'/></li>" +
                "<li>{1}</li>" +
                "</ul>",
                "https://raw.githubusercontent.com/peachpiecompiler/peachpie/master/docs/logos/round-orange-196x196.png",
                "<b>Hello from .NET!</b><br/>The site is powered by .NET Core instead of PHP, compiled entirely to MSIL bytecode using <a href='https://www.peachpie.io/' target='_blank'>PeachPie</a>.");
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

            // add analytics
        }
    }
}
