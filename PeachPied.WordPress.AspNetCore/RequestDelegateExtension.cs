using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;
using Pchp.Core;
using Peachpie.Web;
using PeachPied.WordPress.AspNetCore.Internal;
using PeachPied.WordPress.Sdk;

namespace PeachPied.WordPress.AspNetCore
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension for enabling WordPress.
    /// </summary>
    public static class RequestDelegateExtension
    {
        /// <summary>Redirect to `index.php` if the the file does not exist.</summary>
        static void ShortUrlRule(RewriteContext context, IFileProvider files)
        {
            var req = context.HttpContext.Request;
            var subpath = req.Path.Value;
            if (subpath != "/")
            {
                if (subpath.IndexOf("wp-content/", StringComparison.Ordinal) != -1 ||   // it is in the wp-content -> definitely a file
                    files.GetFileInfo(subpath).Exists ||                            // the script is in the file system
                    Context.TryGetDeclaredScript(subpath.Substring(1)).IsValid ||   // the script is declared (compiled) in Context but not in the file system
                    context.StaticFileProvider.GetFileInfo(subpath).Exists ||       // the script is in the file system
                    subpath == "/favicon.ico") // 404 // even the favicon is not there, let the request proceed
                {
                    // proceed to Static Files
                    return;
                }

                if (files.GetDirectoryContents(subpath).Exists)
                {
                    var lastchar = subpath[subpath.Length - 1];
                    if (lastchar != '/' && lastchar != '\\')
                    {
                        // redirect to the directory with leading slash:
                        context.HttpContext.Response.Redirect(req.PathBase + subpath + "/" + req.QueryString, true);
                        context.Result = RuleResult.EndResponse;
                    }

                    // proceed to default document
                    return;
                }
            }

            // everything else is handled by `index.php`
            req.Path = new PathString("/index.php");
            context.Result = RuleResult.SkipRemainingRules;
        }

        /// <summary>Resolves address of `wp-cron.php` script to be fired on background.</summary>
        static bool TryGetWpCronUri(IServerAddressesFeature addresses, out Uri uri)
        {
            // http://localhost:5004/wp-cron.php?doing_wp_cron

            foreach (var addr in addresses.Addresses)
            {
                if (Uri.TryCreate(addr.Replace("*", "localhost"), UriKind.Absolute, out var addrUri))
                {
                    uri = new Uri(addrUri, "wp-cron.php?doing_wp_cron");
                    return true;
                }
            }

            //
            uri = null;
            return false;
        }

        /// <summary>
        /// Defines WordPress configuration constants and initializes runtime before proceeding to <c>index.php</c>.
        /// </summary>
        static void Apply(Context ctx, WordPressConfig config, params IWpPlugin[] plugins)
        {
            // see wp-config.php:

            // The name of the database for WordPress
            ctx.DefineConstant("DB_NAME", (PhpValue)config.DbName); // define('DB_NAME', 'wordpress');

            // MySQL database username
            ctx.DefineConstant("DB_USER", (PhpValue)config.DbUser); // define('DB_USER', 'root');

            // MySQL database password
            ctx.DefineConstant("DB_PASSWORD", (PhpValue)config.DbPassword); // define('DB_PASSWORD', 'password');

            // MySQL hostname
            ctx.DefineConstant("DB_HOST", (PhpValue)config.DbHost); // define('DB_HOST', 'localhost');

            // disable wp_cron() during the request, we have our own scheduler to fire the job
            ctx.DefineConstant("DISABLE_WP_CRON", PhpValue.True);   // define('DISABLE_WP_CRON', true);

            // $peachpie-wp-loader : WpLoader
            ctx.Globals["peachpie_wp_loader"] = PhpValue.FromClass(new WpLoader(plugins.ConcatSafe(config.Plugins)));
        }

        /// <summary> `WpApp` is compiled in PHP assembly WordPress.dll.</summary>
        static string WordPressAssemblyName => typeof(WpApp).Assembly.FullName;

        /// <summary>
        /// Installs WordPress middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="config">WordPress instance configuration.</param>
        /// <param name="path">Physical location of wordpress folder. Can be absolute or relative to the current directory.</param>
        public static IApplicationBuilder UseWordPress(this IApplicationBuilder app, WordPressConfig config, string path = "wordpress")
        {
            // wordpress root path:
            var root = System.IO.Path.GetFullPath(path);
            var fprovider = new PhysicalFileProvider(root);

            var cachepolicy = new WpResponseCachingPolicyProvider();
            var cachekey = new WpResponseCachingKeyProvider();

            // response caching:
            if (config.EnableResponseCaching)
            {
                app.UseMiddleware<ResponseCachingMiddleware>(cachepolicy, cachekey);
            }

            // url rewriting:
            app.UseRewriter(new RewriteOptions().Add(context => ShortUrlRule(context, fprovider)));

            // handling php files:
            app.UsePhp(new PhpRequestOptions()
            {
                ScriptAssembliesName = WordPressAssemblyName.ArrayConcat(config.LegacyPluginAssemblies),
                BeforeRequest = ctx => Apply(ctx, config, cachepolicy),
                RootPath = root,
            });

            // static files
            app.UseStaticFiles(new StaticFileOptions() { FileProvider = fprovider });

            // fire wp-cron.php asynchronously
            if (TryGetWpCronUri(app.ServerFeatures.Get<IServerAddressesFeature>(), out var wpcronUri))
            {
                WpCronScheduler.StartScheduler(HttpMethods.Post, wpcronUri, TimeSpan.FromSeconds(60));
            }

            //
            return app;
        }
    }
}
