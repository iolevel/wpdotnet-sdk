using Pchp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace PeachPied.WordPress.Standard.Internal
{
    sealed class WordPressOverridesPlugin : IWpPlugin
    {
        #region Registration Validation

        struct ValidationData
        {
            /// <summary>
            /// Object sent from the validate-user api.
            /// </summary>
            sealed class ValidationResponse
            {
                /// <summary>
                /// The user ID.
                /// </summary>
                public int customer { get; set; }

                /// <summary>
                /// The registration email.
                /// </summary>
                public string email { get; set; }

                /// <summary>
                /// The subscription expiration date.s
                /// </summary>
                public DateTime expiration { get; set; }

                /// <summary>
                /// The verification signature for "data".
                /// </summary>
                public byte[] signature { get; set; }
            }

            public DateTime Expiration;

            public byte[] Signature;

            public string Key; // original license key or email, used for automatic validation after expiration

            public static ValidationData FromJson(string response, string key)
            {
                if (string.IsNullOrEmpty(response) || string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException();
                }

                var value = JsonSerializer.Deserialize<ValidationResponse>(response);

                return new ValidationData
                {
                    Expiration = value.expiration,
                    Signature = value.signature,
                    Key = key,
                };
            }

            public static ValidationData FromArray(PhpArray array)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                return new ValidationData
                {
                    Expiration = DateTime.ParseExact(array[nameof(Expiration)].ToString(), "d", DateTimeFormatInfo.InvariantInfo),
                    Signature = JsonSerializer.Deserialize<byte[]>(array[nameof(Signature)].ToString()),
                    Key = array[nameof(Key)].ToString(),
                };
            }

            byte[] ConstructDataString(Uri uri) => Encoding.ASCII.GetBytes($"{uri.Host}@{Expiration.ToString("d", DateTimeFormatInfo.InvariantInfo)}");

            /// <summary>
            /// Verifies the given validation footprint matches current date and domain.
            /// </summary>
            public bool Verify(Uri uri) =>
                uri != null &&
                Signature != null &&
                Expiration > DateTime.UtcNow &&
                Keys.VerifyData(ConstructDataString(uri), Signature);

            /// <summary>Gets data as PHP array.</summary>
            public PhpArray ToArray()
            {
                return new PhpArray(2)
                {
                    { nameof(Expiration), Expiration.ToString("d", DateTimeFormatInfo.InvariantInfo) },
                    { nameof(Signature), JsonSerializer.Serialize(Signature) },
                    { nameof(Key), Key },
                };
            }
        }

        /// <summary>
        /// Gets value indicating the user is properly registered with an active license.
        /// </summary>
        bool IsRegistered => _isRegistered.GetValueOrDefault();

        bool? _isRegistered; // cached value indicating the registration was resolved

        /// <summary>
        /// API for user validation.
        /// </summary>
        static string ValidateUrl => "https://apps.peachpie.io/api/ajax/validate-user?key={0}&domain={1}";

        void AdminNotice(WpApp app, string message, bool success)
        {
            app.OnAdminInit(() =>
            {
                app.AdminNotices(() => $@"<div class='notice notice-{(success ? "success" : "warning")} is-dismissible' style='background:url({LogoUrl}) no-repeat 4px 4px;background-size:2.4em 2.4em;'>
    <p style='margin-left:3em;'><b>WpDotNet:</b> {System.Web.HttpUtility.HtmlEncode(message)}</p>
</div>");
            });
        }

        bool TryRegisterUser(WpApp app)
        {
            string key = null;

            if (app.Context.Post.TryGetValue(RegisterKeyName, out var keyvalue))
            {
                key = keyvalue.AsString();
            }
            else if (app.GetOption(RegisterKeyName).IsPhpArray(out var dataarray))
            {
                try
                {
                    var uri = new Uri(app.GetSiteUrl(), UriKind.Absolute);
                    var data = ValidationData.FromArray(dataarray);
                    if (data.Verify(uri))
                    {
                        _isRegistered = true;
                        AdminNotice(app, $"License is valid until {data.Expiration:d}.", success: true);
                        return true;
                    }
                    else if (data.Expiration <= DateTime.UtcNow && data.Key != null)
                    {
                        // AdminNotice(app, $"License has expired at {data.Expiration.ToString("d")}.", success: false);

                        // try to update the registration automatically
                        key = data.Key;
                    }
                }
                catch
                {
                    // ignore invalid data
                    AdminNotice(app, $"Invalid license data. Please register again.", success: false);
                }

                _isRegistered = false; // do not try to verify again
            }

            // request user validation:

            if (key != null)
            {
                var siteurl = app.GetSiteUrl();
                var uri = new Uri(siteurl, UriKind.Absolute);
                if (TryRequestUserValidation(key, siteurl, out var data))
                {
                    app.UpdateOption(RegisterKeyName, data.ToArray());

                    if (data.Verify(uri))
                    {
                        _isRegistered = true;
                        AdminNotice(app, $"Registration suceeded. Thank you! License is valid until {data.Expiration:d}, then it will be renewed automatically.", success: true);
                        return true;
                    }
                    else
                    {
                        AdminNotice(app, $"License has been verified, although it is no valid. Expiration date is {data.Expiration:d}.", success: false);
                    }
                }
                else
                {
                    AdminNotice(app, $"Invalid license '{key}'. Domain '{uri.Host}' has not been registered.", success: false);
                }
            }

            return false;
        }

        /// <summary>
        /// Requests the <see cref="ValidateUrl"/> and updates user registration.
        /// </summary>
        /// <param name="key">Given license key or email of the subscription.</param>
        /// <param name="url">Current website URL.</param>
        /// <param name="data">Received and validate data.</param>
        /// <returns></returns>
        bool TryRequestUserValidation(string key, string url, out ValidationData data)
        {
            if (!string.IsNullOrEmpty(key) && url != null)
            {
                var req = (HttpWebRequest)WebRequest.Create(string.Format(ValidateUrl, key, url));
                req.Timeout = 5 * 1000;

                try
                {
                    using var r = (HttpWebResponse)req.GetResponse();

                    if (r.StatusCode == HttpStatusCode.OK)
                    {
                        using var reader = new StreamReader(r.GetResponseStream());

                        data = ValidationData.FromJson(reader.ReadToEnd(), key);
                        return true;
                    }
                }
                catch { }
            }

            //
            data = default;
            return false;
        }

        #endregion

        static string WpDotNetUrl => "https://www.wpdotnet.com/";

        static string LogoUrl => "https://raw.githubusercontent.com/iolevel/wpdotnet-sdk/master/wpdotnet.png";

        static string PurchaseLink => WpDotNetUrl + "#purchase";

        static string RegisterBoxId => "wpdotnet-register-box";

        static string RegisterKeyName => "wpdotnet-register-key";

        static string InformationalVersion => typeof(WordPressOverridesPlugin).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        void DashboardRightNow(WpApp app, TextWriter output)
        {
            var frameworkVersion =
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription ??
                Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            string databaseSystem = "MySQL";
            if (!WpStandard.USE_MYSQL)
            {
                databaseSystem = "SQLite (Path: " + WpStandard.DB_DIR + WpStandard.DB_FILE + ")";
            }
            var process = System.Diagnostics.Process.GetCurrentProcess();

            string licenseRow = "";

#if WPDOTNET_LICENSE
            string licensehtml = IsRegistered
                ? $@"<em>{app.GetSiteUrl()} is <a href='{WpDotNetUrl}#registered' target='_blank'>registered</a></em>"
                : $@"<a href='#' onclick='return wpdotnet_register_open();' tooltip=''>Register</a> | <a href='{PurchaseLink}' target='_blank' tooltip=''>Purchase</a>";
            licenseRow = $"<b>License:</b> {licensehtml}<br/>";
#endif

            output.Write($@"
<table style=""border:0;width:100%;""><tbody>
<tr>
    <td><img src=""{LogoUrl}"" style=""width:76px;margin:8px;display:inline;""></td>
    <td>
        <b>Your WordPress runs on {frameworkVersion}</b><br/>
        <b title=""Database System"">Database:</b> {databaseSystem}
        <div>
            <b title=""Memory allocated by the whole process."">Memory usage:</b> {process.WorkingSet64 / 1024 / 1024} MB<br/>
            <b title=""CPU time consumed by the whole process."">CPU usage:</b> {process.TotalProcessorTime:c}<br/>
            {licenseRow}
        </div>
    </td>
</tr>
</tbody></table>");

#if WPDOTNET_LICENSE
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
#endif
        }

        static readonly string s_GeneratorHtml = $"<meta name=\"generator\" content=\"WpDotNet (PeachPie) {InformationalVersion} \" />";

        ValueTask IWpPlugin.ConfigureAsync(WpApp app, CancellationToken token)
        {
#if WPDOTNET_LICENSE
            if (_isRegistered.HasValue == false || app.Context.Post.Count != 0)
            {
                TryRegisterUser(app);
            }
#endif

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

#if WPDOTNET_LICENSE
            if (!IsRegistered)
            {
                app.Footer(output =>
                {
                    output.Write($@"
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
#endif

            //
            return ValueTask.CompletedTask;
        }
    }
}
