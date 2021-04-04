using Pchp.Core;
using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PeachPied.WordPress.DotNet
{
    /// <summary>
    /// Plugin overrides default WP behavior to avoid issues and improve experience when running on .NET.
    /// </summary>
    sealed class DotNetOverridesPlugin : IWpPlugin
    {
        void IWpPlugin.Configure(WpApp app)
        {
            // postpone admin actions
            app.AddFilter("admin_init", new Action(() =>
            {
                app.AddFilter("user_has_cap", new Func<PhpArray, PhpArray, PhpArray, PhpArray>((allcaps, cap, args) =>
                {
                    allcaps["update_core"] = false;
                    allcaps["update_php"] = false;
                    //
                    return allcaps;
                }), accepted_args: 3);

                app.AddFilter("pre_site_transient_update_core", new Func<stdClass>(() =>
                {
                    // TODO: wpdotnet check for version update
                    return new PhpArray()
                    {
                        { "last_checked", Pchp.Library.DateTime.DateTimeFunctions.time() },
                        { "version_checked", app.Context.Globals["wp_version"].ToString() },
                    }.ToObject();

                }), accepted_args: 0);
            }));

            // ensure "plugins" directory exists, otherwise eventual mkdir() in wp fails
            if (app.Context.TryGetConstant("WP_PLUGIN_DIR", out var plugindir))
            {
                try { Directory.CreateDirectory(plugindir.ToString(app.Context)); }
                catch { }
            }
        }
    }
}
