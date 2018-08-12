using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PeachPied.WordPress.Sdk.Internal
{
    sealed class WordPressOverridesPlugin : IWpPlugin
    {
        public static readonly string InformationalVersion = typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        bool IsLicensed()
        {
            return false;
        }

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

        void WpDotNetLicense(TextWriter output)
        {
            output.Write(
                "Thanks for using WordPress on .NET!<br/>" +
                (IsLicensed()
                    ? "Your copy has been activated successfuly"
                    : "<input id='license' name='license' type='text' placeholder='license key' /><input type='submit' value='Activate' />")
            );
        }

        void WpDotNetFooter(TextWriter output)
        {
            output.Write(
                "<div style='position:fixed;z-index:5000;bottom:0;width:100%;color:#fff;background:#333;padding:4px;text-align:center;'>" +
                    "Hey there! Why not register and keep in touch. This site is entirely powered by .NET giving your web cool abilities. " +
                    "<button type='button'>Go to dashboard</button>" +
                "</div>"
            );
        }

        static readonly string GeneratorHtml = "<meta name=\"generator\" content=\"WpDotNet (PeachPie) " + InformationalVersion + " \" />";

        public void Configure(WpApp app)
        {
            //
            // Dashboard:
            //

            // add information into Right Now section
            app.DashboardRightNow(DashboardRightNow);

            // // add license dashboard widget
            // app.DashboardWidget("wpdotnet_license_widget", "WpDotNet License", WpDotNetLicense);

            // // add license footer
            // if (!IsLicensed())
            // {
            //     app.AddFilter("wp_footer", new Action(() => WpDotNetFooter(app.Context.Output)));
            // }

            // do not allow editing of .php files:
            app.AddFilter("editable_extensions", new Func<IList, IList>(editable_extensions =>
            {
                editable_extensions.Remove("php");
                return editable_extensions;
            }));

            // TODO: "install_plugins_upload" to customize upload plugin form
            // TODO: filter query_plugins

            //
            // Blogs:
            //

            // alter generator metadata
            app.AddFilter("get_the_generator_xhtml", new Func<string>(() => GeneratorHtml));
            // add analytics
        }
    }
}
