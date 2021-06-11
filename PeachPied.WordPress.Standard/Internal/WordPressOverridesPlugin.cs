using Pchp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
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
        static string WpDotNetUrl => "https://www.wpdotnet.com/";

        static string ValidateUrl => "https://apps.peachpie.io/api/ajax/validate-user?key={0}&domain={1}";

        static string LogoUrl => "https://raw.githubusercontent.com/iolevel/wpdotnet-sdk/master/wpdotnet.png";

        static string PurchaseLink => WpDotNetUrl + "#purchase";

        static string RegisterBoxId => "wpdotnet-register-box";

        static string RegisterKeyName => "wpdotnet-register-key";

        static string InformationalVersion => typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        bool? _registered;

        bool IsRegistered => _registered.GetValueOrDefault();

        bool ValidateUser(string key, string domain)
        {
            var req = (HttpWebRequest)WebRequest.Create(string.Format(ValidateUrl, key, domain));
            req.Timeout = 5 * 1000;
            try
            {
                using (var r = (HttpWebResponse)req.GetResponse())
                {
                    if (r.StatusCode == HttpStatusCode.OK)
                    {
                        _registered = true;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        void DashboardRightNow(WpApp app, TextWriter output)
        {
            var frameworkVersion =
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription ??
                Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            var process = System.Diagnostics.Process.GetCurrentProcess();

            string licensehtml = IsRegistered
                ? $@"<em>{app.GetSiteUrl()} is <a href='{WpDotNetUrl}#registered' target='_blank'>registered</a></em>"
                : $@"<a href='#' onclick='return wpdotnet_register_open();' tooltip=''>Register</a> | <a href='{PurchaseLink}' target='_blank' tooltip=''>Purchase</a>";

            output.Write($@"
<table style=""border:0;width:100%;""><tbody>
<tr>
    <td><img src=""{LogoUrl}"" style=""width:76px;margin:8px;display:inline;""></td>
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

            if (!IsRegistered)
            {
                output.Write($@"
<div id='{RegisterBoxId}' style='display:none;'>
    <hr/>
     <p>
        <p style=''>Please register your domain <b>{app.GetSiteUrl()}</b> using your purchase email or license key.</p>
        <form method='post'>
            <table style='border:0;width:100%;'><tbody>
            <tr>
                <td><b>E-mail or license key:</b></td>
                <td><input name='{RegisterKeyName}' type='text' class='regular-text' style='width:100%;' placeholder='name@example.org' required='required' value='{app.GetAdminEmail()}'></input></td>
            </tr>
            <tr>
                <td></td>
                <td style='text-align:right;'><input type='submit' class='button' value='Register' /></td>
            </tr>
            </tbody></table>
        </form>
     </p>
</div>
");
            }

        }

        static readonly string s_GeneratorHtml = $"<meta name=\"generator\" content=\"WpDotNet (PeachPie) {InformationalVersion} \" />";

        public void Configure(WpApp app)
        {
            if (_registered.HasValue == false)
            {
                if (app.Context.Post.TryGetValue(RegisterKeyName, out var keyvalue) && keyvalue.IsString(out var key))
                {
                    ValidateUser(key, app.GetSiteUrl());
                    app.UpdateOption(RegisterKeyName, key);
                }
            }

            //
            // Dashboard:
            //

            // add information into Right Now section
            app.DashboardRightNow(output => DashboardRightNow(app, output));

            //
            // Blogs:
            //

            // alter generator metadata
            app.AddFilter("get_the_generator_xhtml", new Func<string>(() => s_GeneratorHtml));

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

            if (!IsRegistered)
            {
                app.Footer(output =>
                {
                    output.Write(@$"
<div style='position:fixed;text-align:center; width:192px; height:50px; bottom:0; right:0;padding:12px 0 12px 50px; margin:0 auto; vertical-align: middle;background:#68b227;background-image:url({LogoUrl});background-size:50px 50px;background-repeat:no-repeat;border-left:solid 10px rgba(255,255,255,.5);line-height:24px;' title='WpDotNet'>
    <a href='{WpDotNetUrl}?from=footer' style='color:#fff;font-size:16px;font-style:tahoma,helvetica;padding:0; margin:0;'>WpDotNet</a>
</div>
");
                });

                app.OnAdminInit(() =>
                {
                    app.AddFilter("admin_print_footer_scripts", new Action(() =>
                    {
                        app.Context.Output.Write($@"
<script type='text/javascript'>
    function wpdotnet_register_open() {{
        jQuery('#{RegisterBoxId}').slideToggle('fast');
        jQuery('#{RegisterBoxId} input[type=""text""]').focus();
        return false;
    }}
</script>
");
                    }));
                });
            }
        }
    }
}
