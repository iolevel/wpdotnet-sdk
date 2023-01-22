using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pchp.Core;
using Peachpie.AspNetCore.Web;
using PeachPied.WordPress.AspNetCore;
using PeachPied.WordPress.AspNetCore.Internal;
using PeachPied.WordPress.Standard;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension for enabling WordPress.
    /// </summary>
    public static class RequestDelegateExtension
    {
        /// <summary>Redirect to `index.php` if the the file does not exist.</summary>
        static void ShortUrlRule(RewriteContext context, IFileProvider files, bool multisite = false, bool  subdomainInstall = false, string siteurl = null)
        {
            var req = context.HttpContext.Request;
            var subpath = req.Path.Value;
            var basepath = req.PathBase.Value;

            // serve WordPress files if the WordPress path is a part of the requested path.
            if (subpath != "/" && subpath.Length != 0 && (String.IsNullOrEmpty(siteurl) || siteurl == "/" || basepath == siteurl))
            {
                if (multisite)
                {
                    // add a trailing slash to /wp-admin
                    if ((subdomainInstall && subpath == "/wp-admin") ||
                        (!subdomainInstall && subpath.EndsWith("/wp-admin")))
                    {
                        context.HttpContext.Response.Redirect(req.PathBase + subpath + "/" + req.QueryString, true);
                        context.Result = RuleResult.EndResponse;
                        return;
                    }
                }

                if ((!multisite && subpath.IndexOf("wp-content/", StringComparison.Ordinal) != -1) ||   // it is in the wp-content -> definitely a file (When multisite is enabled, it requires rewriting the url)
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

                if (multisite)
                {
                    if (subdomainInstall)
                    {
                        if (subpath.StartsWith("/wp-content", StringComparison.Ordinal) ||
                            subpath.StartsWith("/wp-admin", StringComparison.Ordinal) ||
                            subpath.StartsWith("/wp-includes", StringComparison.Ordinal))
                        {
                            // proceed to default document
                            return;
                        }
                    }
                    else
                    {

                        int shortcut = subpath.IndexOf("wp-content", StringComparison.Ordinal);
                        if (shortcut == -1)
                            shortcut = subpath.IndexOf("wp-admin", StringComparison.Ordinal);
                        if (shortcut == -1)
                            shortcut = subpath.IndexOf("wp-includes", StringComparison.Ordinal);

                        if (shortcut != -1)
                        {
                            // remove site path and proceed to default document
                            req.Path = new PathString("/" + subpath.Remove(0, shortcut));
                            return;
                        }
                    }

                    if (subpath.EndsWith(".php"))
                    {
                        if (!subdomainInstall)
                        {
                            // remove site path
                            req.Path = new PathString(subpath.Remove(0, subpath.LastIndexOf('/')));
                            subpath = req.Path.Value;
                        }

                        if (files.GetFileInfo(subpath).Exists ||                            // the script is in the file system
                            Context.TryGetDeclaredScript(subpath.Substring(1)).IsValid ||   // the script is declared (compiled) in Context but not in the file system
                            context.StaticFileProvider.GetFileInfo(subpath).Exists)         // the script is in the file system)
                        {
                            return;
                        }
                    }
                }

            }

            // everything else is handled by `index.php`
            req.Path = new PathString("/index.php");
            context.Result = RuleResult.SkipRemainingRules;
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

            if (!string.IsNullOrEmpty(config.SiteUrl))
            {
                ctx.DefineConstant("WP_SITEURL", config.SiteUrl);
            }

            if (!string.IsNullOrEmpty(config.HomeUrl))
            {
                ctx.DefineConstant("WP_HOME", config.HomeUrl);
            }

            // multisite
            if (config.Multisite.Allow)
                ctx.DefineConstant("WP_ALLOW_MULTISITE", (PhpValue)config.Multisite.Allow);
            
            if (config.Multisite.Enable)
            {
                ctx.DefineConstant("MULTISITE", (PhpValue)config.Multisite.Enable);
                ctx.DefineConstant("SUBDOMAIN_INSTALL", (PhpValue)config.Multisite.DomainCurrentSite);
                ctx.DefineConstant("DOMAIN_CURRENT_SITE", (PhpValue)config.Multisite.DomainCurrentSite);
                ctx.DefineConstant("PATH_CURRENT_SITE", (PhpValue)config.Multisite.PathCurrentSite);
                ctx.DefineConstant("SITE_ID_CURRENT_SITE", (PhpValue)config.Multisite.SiteIDCurrentSite);
                ctx.DefineConstant("BLOG_ID_CURRENT_SITE", (PhpValue)config.Multisite.BlogIDCurrentSite);
            }

            // Additional constants
            if (config.Constants != null)
            {
                foreach (var pair in config.Constants)
                {
                    ctx.DefineConstant(pair.Key, pair.Value);
                }
            }

            // $peachpie-wp-loader : WpLoader
            ctx.Globals["peachpie_wp_loader"] = PhpValue.FromClass(loader);

            // workaround HTTPS under proxy,
            // set $_SERVER['HTTPS'] = 'on'
            // https://wordpress.org/support/article/administration-over-ssl/#using-a-reverse-proxy
            if (ctx.IsWebApplication && ctx.GetHttpContext().Request.Headers["X-Forwarded-Proto"] == "https")
            {
                ctx.Server["HTTPS"] = "on";
            }
        }

        /// <summary>
        /// Installs WordPress middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">Physical location of wordpress folder. Can be absolute or relative to the current directory.</param>
        /// <param name="configure">Optional callback invoked every request when rendering WordPress page. Shortcut for implementing a plugin (<see cref="IWpPlugin"/>).</param>
        public static IApplicationBuilder UseWordPress(this IApplicationBuilder app, string path = null, Action<WpApp> configure = null)
        {
            // load options
            var options = new WordPressConfig()
                .LoadFromSettings(app.ApplicationServices)      // appsettings.json
                .LoadFromEnvironment(app.ApplicationServices)   // environment variables (known cloud hosts)
                .LoadFromOptions(app.ApplicationServices)       // IConfigureOptions<WordPressConfig> service
                .LoadDefaults();    // 

            //
            if (configure != null)
            {
                options.PluginContainer.Add(new WpPluginAsCallback(configure));
            }

            // get WP_HOME and WP_SITE
            string sitepath = null;
            string homepath = null;

            if (Uri.TryCreate(options.SiteUrl, UriKind.Absolute, out var siteuri))
            {
                sitepath = siteuri.LocalPath;
            }
            
            if (Uri.TryCreate(options.HomeUrl, UriKind.Absolute, out var homeuri))
            {
                homepath = homeuri.LocalPath;
            }

            bool siteProceed = false;
            // use when the wordpress path is a part of the requested path
            app.UseWhen(
                context =>
                {
                    siteProceed = string.IsNullOrEmpty(sitepath) || context.Request.Path.Value.StartsWith(sitepath);
                    return siteProceed;
                },
                app => app.UsePathBase(sitepath).InstallWordPress(options, path, sitepath)
            );

            // use when the home path is a part of the requested path
            app.UseWhen(
                context => !siteProceed && (string.IsNullOrEmpty(homepath) || context.Request.Path.Value.StartsWith(homepath)),
                app => app.UsePathBase(homepath).InstallWordPress(options, path, sitepath)
            );

            //
            return app;
        }

        private static IApplicationBuilder InstallWordPress(this IApplicationBuilder app, WordPressConfig options, string path = null, string siteurl = null)
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

            // list of plugins:
            var plugins = new WpPluginContainer(options.PluginContainer);

            // response caching:
            if (options.EnableResponseCaching)
            {
                // var cachepolicy = new WpResponseCachingPolicyProvider();
                // var cachekey = app.ApplicationServices.GetService<WpResponseCachingKeyProvider>();

                var cachepolicy = new WpResponseCachePolicy();
                plugins.Add(cachepolicy);

                // app.UseMiddleware<ResponseCachingMiddleware>(cachepolicy, cachekey);
                app.UseMiddleware<WpResponseCacheMiddleware>(new MemoryCache(new MemoryCacheOptions { }), cachepolicy);
            }

            // if (options.LegacyPluginAssemblies != null)
            // {
            //     options.LegacyPluginAssemblies.ForEach(name => Context.AddScriptReference(Assembly.Load(new AssemblyName(name))));
            // }

            var wploader = new WpLoader(plugins:
                CompositionHelpers.GetPlugins(options.CompositionContainers.CreateContainer(), app.ApplicationServices, root)
                .Concat(plugins.GetPlugins(app.ApplicationServices)));

            // url rewriting:
            bool multisite = options.Multisite.Enable;
            bool subdomainInstall = options.Multisite.SubdomainInstall;
            if (options.Constants != null && !multisite && !subdomainInstall)
            {
                // using constants instead MultisiteData
                if (options.Constants.TryGetValue("MULTISITE", out string multi))
                    multisite = bool.TryParse(multi, out bool multiConverted) && multiConverted;
                if (options.Constants.TryGetValue("SUBDOMAIN_INSTALL", out string domain))
                    subdomainInstall = bool.TryParse(domain, out bool domainConverted) && domainConverted;
            }
            app.UseRewriter(new RewriteOptions().Add(context => ShortUrlRule(context, fprovider, multisite, subdomainInstall, siteurl)));

            // update globals used by WordPress:
            WpStandard.DB_HOST = options.DbHost;
            WpStandard.DB_NAME = options.DbName;
            WpStandard.DB_PASSWORD = options.DbPassword;
            WpStandard.DB_USER = options.DbUser;

            //
            var env = app.ApplicationServices.GetService<IWebHostEnvironment>();
            WpStandard.WP_DEBUG = options.Debug || env.IsDevelopment();

            // handling php files:
            var startup = new Action<Context>(ctx =>
            {
                Apply(ctx, options, wploader);
            });

            // app.UsePhp(
            //     prefix: default, // NOTE: maybe we can handle only index.php and wp-admin/*.php ?
            //     configureContext: startup,
            //     rootPath: root);

            app.UsePhp(new PhpRequestOptions()
            {
                ScriptAssembliesName = options.LegacyPluginAssemblies?.ToArray(),
                BeforeRequest = startup,
                RootPath = root,
            });

            // static files
            app.UseStaticFiles(new StaticFileOptions() { FileProvider = fprovider });

            // fire wp-cron.php asynchronously
            WpCronScheduler.StartScheduler(startup, TimeSpan.FromSeconds(120), root);

            return app;
        }
    }
}
