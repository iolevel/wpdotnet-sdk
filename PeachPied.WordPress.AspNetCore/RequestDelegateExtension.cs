using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Pchp.Core;
using Peachpie.AspNetCore.Web;
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
        static void Apply(Context ctx, WordPressConfig config, WpLoader loader)
        {
            // see wp-config.php:

            // WordPress Database Table prefix.
            ctx.Globals["table_prefix"] = (PhpValue)config.DbTablePrefix; // $table_prefix  = 'wp_';

            // SALT
            ctx.DefineConstant("AUTH_KEY", (PhpValue)config.SALT.AUTH_KEY);
            ctx.DefineConstant("SECURE_AUTH_KEY", (PhpValue)config.SALT.SECURE_AUTH_KEY);
            ctx.DefineConstant("LOGGED_IN_KEY", (PhpValue)config.SALT.LOGGED_IN_KEY);
            ctx.DefineConstant("NONCE_KEY", (PhpValue)config.SALT.NONCE_KEY);
            ctx.DefineConstant("AUTH_SALT", (PhpValue)config.SALT.AUTH_SALT);
            ctx.DefineConstant("SECURE_AUTH_SALT", (PhpValue)config.SALT.SECURE_AUTH_SALT);
            ctx.DefineConstant("LOGGED_IN_SALT", (PhpValue)config.SALT.LOGGED_IN_SALT);
            ctx.DefineConstant("NONCE_SALT", (PhpValue)config.SALT.NONCE_SALT);

            // Additional constants
            if (config.Constants != null)
            {
                foreach (var pair in config.Constants)
                {
                    ctx.DefineConstant(pair.Key, pair.Value);
                }
            }

            // disable wp_cron() during the request, we have our own scheduler to fire the job
            ctx.DefineConstant("DISABLE_WP_CRON", PhpValue.True);   // define('DISABLE_WP_CRON', true);

            // $peachpie-wp-loader : WpLoader
            ctx.Globals["peachpie_wp_loader"] = PhpValue.FromClass(loader);
        }

        /// <summary>Class <see cref="WP"/> is compiled in PHP assembly <c>Peachpied.WordPress.dll</c>.</summary>
        static string WordPressAssemblyName => typeof(WP).Assembly.FullName;

        /// <summary>
        /// Installs WordPress middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="config">WordPress instance configuration.</param>
        /// <param name="plugins">Container describing what plugins will be loaded.</param>
        /// <param name="path">Physical location of wordpress folder. Can be absolute or relative to the current directory.</param>
        public static IApplicationBuilder UseWordPress(this IApplicationBuilder app, WordPressConfig config = null, WpPluginContainer plugins = null, string path = null)
        {
            // wordpress root path:
            if (path == null)
            {
                // bin/wordpress
                path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/wordpress";

                if (Directory.Exists(path) == false)
                {
                    // cwd/wordpress
                    path = Path.GetDirectoryName(Directory.GetCurrentDirectory()) + "/wordpress";

                    if (Directory.Exists(path) == false)
                    {
                        // cwd/../wordpress
                        path = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())) + "/wordpress";
                    }
                }
            }

            var root = System.IO.Path.GetFullPath(path);
            var fprovider = new PhysicalFileProvider(root);

            // log exceptions:
            app.UseDiagnostic();

            // plugins & configuration
            plugins = new WpPluginContainer(plugins);

            if (config == null)
            {
                config = WpConfigurationLoader
                    .CreateDefault()
                    .LoadFromSettings(app.ApplicationServices);
            }

            config.LoadFromEnvironment(app.ApplicationServices);

            // response caching:
            if (config.EnableResponseCaching)
            {
                // var cachepolicy = new WpResponseCachingPolicyProvider();
                // var cachekey = app.ApplicationServices.GetService(typeof(WpResponseCachingKeyProvider));
                
                var cachepolicy = new WpResponseCachePolicy();
                plugins.Add(cachepolicy);

                // app.UseMiddleware<ResponseCachingMiddleware>(cachepolicy, cachekey);
                app.UseMiddleware<WpResponseCacheMiddleware>(new MemoryCache(new MemoryCacheOptions{}), cachepolicy);
            }

            // update globals
            WpStandard.DB_HOST = config.DbHost;
            WpStandard.DB_NAME = config.DbName;
            WpStandard.DB_PASSWORD = config.DbPassword;
            WpStandard.DB_USER = config.DbUser;

            //
            var env = (IHostingEnvironment)app.ApplicationServices.GetService(typeof(IHostingEnvironment));
            WpStandard.WP_DEBUG = config.Debug || env.IsDevelopment();

            var wploader = new WpLoader(CompositionHelpers.GetPlugins(app.ApplicationServices).Concat(plugins.GetPlugins(app.ApplicationServices)));

            // url rewriting:
            app.UseRewriter(new RewriteOptions().Add(context => ShortUrlRule(context, fprovider)));
            
            // handling php files:
            app.UsePhp(new PhpRequestOptions()
            {
                ScriptAssembliesName = WordPressAssemblyName.ArrayConcat(config.LegacyPluginAssemblies),
                BeforeRequest = ctx => Apply(ctx, config, wploader),
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
