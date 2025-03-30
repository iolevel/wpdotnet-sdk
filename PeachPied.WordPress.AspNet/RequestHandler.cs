using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Web;
using Pchp.Core;
using Pchp.Core.Utilities;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNet
{
    /// <summary>
    /// Request handling for wordpress scripts.
    /// </summary>
    public class RequestHandler : Peachpie.RequestHandler.RequestHandler
    {
        /// <summary>
        /// Creates request context for a WordPress application.
        /// </summary>
        protected override Context InitializeRequestContext(HttpContext context)
        {
            var ctx = base.InitializeRequestContext(context);

            ctx.RootPath = ctx.WorkingDirectory = 
                // where the nuget package resolves the contentFiles,
                // this should be placed outside the bin folder tho:
                Path.Combine(context.Request.PhysicalApplicationPath, "bin/wordpress");

            ApplySettings(ctx, context.GetSection("wordpress") as NameValueCollection);

            return ctx;
        }

        void ApplySettings(Context ctx, NameValueCollection configuration)
        {
            WpStandard.WP_DEBUG = Debugger.IsAttached;

            var table_prefix = "wp_";

            if (configuration != null)
            {
                for (int i = 0; i < configuration.Count; i++)
                {
                    var key = configuration.GetKey(i);
                    var value = configuration.Get(i);

                    switch (key.ToUpperInvariant())
                    {
                        case "TABLE_PREFIX": table_prefix = value; break;
                        case "DB_HOST": WpStandard.DB_HOST = value; break;
                        case "DB_NAME": WpStandard.DB_NAME = value; break;
                        case "DB_USER": WpStandard.DB_USER = value; break;
                        case "DB_PASSWORD": WpStandard.DB_PASSWORD = value; break;
                        case "WP_DEBUG": WpStandard.WP_DEBUG = bool.Parse(value); break;
                        //case "DISABLE_WP_CRON": WpStandard.DISABLE_WP_CRON = bool.Parse(value); break;
                        // TODO: WP_CONTENT_DIR: resolve absolute path relatively to app root
                        default:
                            ctx.DefineConstant(key, value);
                            break;
                    }
                }
            }

            // required global variables:
            ctx.Globals["table_prefix"] = table_prefix;
        }
    }
}
