using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Pchp.Core;
using Peachpied.WordPress.AspNetCore.Marketplace.Protocol;
using PeachPied.WordPress.AspNetCore;
using PeachPied.WordPress.Sdk;

namespace Peachpied.WordPress.AspNetCore.Marketplace
{
    [Export(typeof(IWpPluginProvider))]
    sealed class Provider : IWpPluginProvider
    {
        IEnumerable<IWpPlugin>/*!!*/IWpPluginProvider.GetPlugins(IServiceProvider provider)
        {
            yield return new PluginsOverride();
        }
    }

    /// <summary>
    /// Plugin information object.
    /// </summary>
    [DebuggerDisplay("Plugin: {slug,nq} {version,nq}")]
    sealed class PluginResult
    {
        static string DefaultIconFormat => "https://ps.w.org/{0}/assets/icon-256x256.png";
        static Dictionary<string, string> _slugToIdentity = new Dictionary<string, string>(); // not needed anymore, just for sure

        static string IdToSlug(PackageIdentity identity)
        {
            var slug = identity.Id.ToLowerInvariant().Replace('.', '-').Replace("--", "-");
            _slugToIdentity[slug] = identity.Id;
            return slug;
        }

        public static string SlugToId(string slug)
        {
            return _slugToIdentity.TryGetValue(slug, out var id) ? id : slug;
        }

        public PluginResult(RawPackageSearchMetadata p)
        {
            this.name = p.Title;
            this.slug = IdToSlug(p.Identity); //$"{p.Identity.Id},{p.Identity.}";
            this.downloaded = p.DownloadCount ?? 0;

            this.icons = new PhpArray { { "2x", p.IconUrl?.AbsoluteUri ?? string.Format(DefaultIconFormat, this.slug) } };

            this.short_description = p.Summary;
            this.sections = new PhpArray { { "description", p.Description }, };

            if (p.Published.HasValue)
            {
                this.added = p.Published.Value.ToString("R");
            }

            if (p.RawVersions != null && p.RawVersions.Length != 0)
            {
                var lastupdate = p.RawVersions
                    .Select(x => x.Published.HasValue ? x.Published.Value : DateTime.MinValue)
                    .Max();
                this.last_updated = lastupdate.ToString("R");
            }
            //this.active_installs = -1;
            //this.rating = ...

            this.tags = new PhpArray((p.Tags ?? string.Empty).Split(new[] { ',' }));
            this.version = p.Identity.Version.ToNormalizedString();
            this.author = p.Authors;
            this.homepage = p.ProjectUrl?.AbsoluteUri;

            if (p.Identity is SourcePackageDependencyInfo srcpkg)
            {
                this.download_link = srcpkg.DownloadUri.AbsoluteUri;
            }
        }

        public string name;
        public string slug;
        public string version;
        public string author; // HTML: <a href.../>
        public string author_profile = null; // URL
        public string requires = null;
        public string tested = null;
        public string requires_php = null;

        public int rating = 100; // 0 - 100
        public PhpArray ratings = new PhpArray(5);
        public int num_ratings = 0;

        public int active_installs = 1_000_000;
        public long downloaded;

        public string last_updated; // date
        public string added; // date
        public string homepage; // URL
        public string group = null;

        public PhpArray sections; // [description, installation, faq, changelog, screenshots]
        public PhpArray banners = new PhpArray(2); // [low => URL, high => URL]
        public PhpArray contributors = new PhpArray(); // [username => profile_URL]

        public string short_description; // text
        public string download_link; // URL
        public PhpArray screenshots = null; // ??
        public PhpArray tags;
        public PhpArray versions = null;  // [version => download_link] // ??
        public string donate_link = string.Empty; // URL or ""
        public PhpArray icons; // [1x => URL, 2x => URL]

        public bool external = true;
    }

    sealed class QueryPluginsResult
    {
        public PhpArray info; // [page, pages, results]
        public PhpArray plugins; // list of PluginResult
        public bool external = true;
    }

    [DebuggerDisplay("Theme: {slug,nq} {version,nq}")]
    sealed class ThemeResult
    {
        public ThemeResult(RawPackageSearchMetadata p)
        {
            this.name = p.Title;
            this.slug = p.Identity.Id; //$"{p.Identity.Id},{p.Identity.}";
            this.downloaded = p.DownloadCount ?? 0;
            this.version = p.Identity.Version.ToNormalizedString();
            this.author = p.Authors;
            this.homepage = p.ProjectUrl?.AbsoluteUri;
            this.description = p.Description;
            this.screenshot_url = p.IconUrl?.AbsoluteUri;

            if (p.RawVersions != null && p.RawVersions.Length != 0)
            {
                var lastupdate = p.RawVersions
                    .Select(x => x.Published.HasValue ? x.Published.Value : DateTime.MinValue)
                    .Max();
                this.last_updated = lastupdate.ToString("R");
            }

            if (p.Identity is SourcePackageDependencyInfo srcpkg)
            {
                this.download_link = srcpkg.DownloadUri.AbsoluteUri;
            }

            // ...
        }

        public string name;
        public string slug;
        public string version;
        public string preview_url;
        public string author;
        public string screenshot_url;
        public double rating = 100;
        public long num_ratings = 0;
        public long downloaded = 0;
        public string last_updated = DateTime.UtcNow.ToString("R");
        public string homepage;
        public string description;
        public string download_link;

        public string install_url; // set by wp
        public string activate_url; // set by wp
        public string customize_url; // set by wp
        public PhpValue stars = PhpValue.Null; // set by wp
    }

    sealed class QueryThemesResult
    {
        public PhpArray info; // [page : int, pages : int, results : int]
        public PhpArray themes; // ThemeResult[]
    }

    sealed class PluginsOverride : IWpPlugin
    {
        static string NuGetFeed => "https://peachpie-feed.azurewebsites.net/v3/index.json";
        readonly PackagesHelper _packages = new PackagesHelper();
        static string[] WpPluginPackageType => new[] { "WpPlugin,1.0.0.9" };
        static string[] WpThemePackageType => new[] { "WpTheme,1.0.0.9" };

        SourceRepository SourceRepository
        {
            get
            {
                // TODO: Cache

                List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
                providers.AddRange(Repository.Provider.GetCoreV3());
                //providers.AddRange(Repository.Provider.GetCoreV2());

                var packageSource = new PackageSource(NuGetFeed);
                var sourceRepository = new SourceRepository(packageSource, providers);

                return sourceRepository;
            }
        }

        PackageSearchResource PackageSearchResource => SourceRepository.GetResource<PackageSearchResource>();
        RegistrationResourceV3 RegistrationResourceV3 => SourceRepository.GetResource<RegistrationResourceV3>();
        RawSearchResourceV3 RawSearchResourceV3 => SourceRepository.GetResource<RawSearchResourceV3>();
        ServiceIndexResourceV3 ServiceIndexResourceV3 => SourceRepository.GetResource<ServiceIndexResourceV3>();

        public PluginsOverride()
        {
            _packages.LoadPackages();
        }

        static RawPackageSearchMetadata PackageSearchMetadataFromJObject(JObject metadata)
        {
            var p = metadata.FromJToken<RawPackageSearchMetadata>();

            if (p.RawVersions == null)
            {
                var versions = metadata[JsonProperties.Versions];
                if (versions != null)
                {
                    p.RawVersions = versions.FromJToken<RawVersionInfo[]>();
                }
            }

            //
            return p;
        }

        IEnumerable<RawPackageSearchMetadata> SearchFeed(PhpArray wp_args, string[] packageTypes, out int page, out int per_page, out int total_count)
        {
            var log = NuGet.Common.NullLogger.Instance;

            page = (int)wp_args["page"] - 1;
            per_page = (int)wp_args["per_page"];

            // arr[browse|search|author|tag]
            var browse = wp_args["browse"].AsString();
            var searchTerm = wp_args["search"].AsString() ?? wp_args["author"].AsString() ?? wp_args["tag"].AsString() ?? string.Empty;

            if (browse != null)
            {
                switch (browse)
                {
                    case "featured":
                    case "popular":
                    case "new":         // theme
                    case "favorites":   // theme
                    case "beta":
                    case "recommended": // = premium
                    default:
                        break;
                }
            }

            var searchFilter = new SearchFilter(includePrerelease: browse == "beta" || true/*always beta*/)
            {
                PackageTypes = packageTypes,
            };

            //var results = PackageSearchResource.SearchAsync(
            //    "", new SearchFilter(true), 
            //    skip: page * per_page, take: per_page, log: null, cancellationToken: CancellationToken.None).Result.ToList();

            var raw = RawSearchResourceV3.SearchPage(searchTerm, searchFilter, page * per_page, per_page, log, CancellationToken.None).Result;
            var items = Enumerable.Empty<RawPackageSearchMetadata>();

            if (raw[JsonProperties.Data] is JArray jarr)
            {
                items = jarr.OfType<JObject>().Select(PackageSearchMetadataFromJObject);
            }

            total_count = raw["totalHits"].ToObject<int>();

            return items;
        }

        PhpValue/*object|array|WP_Error*/PluginsApi(PhpValue result, string action, object args)
        {
            var arr = (PhpArray)PhpValue.FromClass(args);
            var log = NuGet.Common.NullLogger.Instance;

            switch (action)
            {
                case "query_plugins":

                    var results = SearchFeed(arr, WpPluginPackageType, out var page, out var per_page, out var totalHits).ToList();

                    return PhpValue.FromClass(new QueryPluginsResult
                    {
                        info = new PhpArray()
                        {
                            {"page", page},
                            {"pages", (totalHits / per_page) + ((totalHits % per_page) == 0 ? 0 : 1)},
                            {"results", results.Count},
                        },
                        plugins = new PhpArray(results.Select(_ => new PluginResult(_))),
                    });

                case "plugin_information":

                    var versions = RegistrationResourceV3.GetPackageMetadata(PluginResult.SlugToId(arr["slug"].ToString()), true, true, log, CancellationToken.None)
                        .Result
                        .Select(PackageSearchMetadataFromJObject);
                    var p = versions.LastOrDefault();

                    //var p = PackageMetadataResource.GetMetadataAsync(new PackageIdentity(arr["slug"].ToString(), new NuGet.Versioning.NuGetVersion("")), log, CancellationToken.None).Result;
                    if (p != null)
                    {
                        var packageBaseAddress = ServiceIndexResourceV3.GetServiceEntryUri(ServiceTypes.PackageBaseAddress)?.AbsoluteUri;
                        var id = p.Identity.Id.ToLowerInvariant();
                        var version = p.Identity.Version.ToNormalizedString().ToLowerInvariant();
                        var url = $"{packageBaseAddress}/{id}/{version}/{id}.{version}.nupkg";

                        return PhpValue.FromClass(new PluginResult(p)
                        {
                            download_link = url
                        });
                    }
                    else
                    {
                        return PhpValue.Null;
                    }

                case "hot_tags":
                case "hot_categories":
                    return PhpValue.Null;

                default:
                    throw new ArgumentException(nameof(action));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="override"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        PhpValue/*object|array|WP_Error*/ThemesApi(PhpValue/*false|object|array*/@override, string action, object args)
        {
            var arr = (PhpArray)PhpValue.FromClass(args);
            var log = NuGet.Common.NullLogger.Instance;

            if (action == "query_themes")
            {
                var results = SearchFeed(arr, WpThemePackageType, out var page, out var per_page, out var totalHits).ToList();

                // return new QueryThemesResult;
                return PhpValue.FromClass(new QueryThemesResult
                {
                    info = new PhpArray()
                    {
                        {"page", page},
                        {"pages", (totalHits / per_page) + ((totalHits % per_page) == 0 ? 0 : 1)},
                        {"results", results.Count},
                    },
                    themes = new PhpArray(results.Select(_ => new ThemeResult(_))),
                });
            }
            else if (action == "theme_information")
            {
                var p = RegistrationResourceV3.GetPackageMetadata(PluginResult.SlugToId(arr["slug"].ToString()), true, true, log, CancellationToken.None)
                        .Result
                        .Select(PackageSearchMetadataFromJObject)
                        .FirstOrDefault();

                if (p != null)
                {
                    var packageBaseAddress = ServiceIndexResourceV3.GetServiceEntryUri(ServiceTypes.PackageBaseAddress)?.AbsoluteUri;
                    var id = p.Identity.Id.ToLowerInvariant();
                    var version = p.Identity.Version.ToNormalizedString().ToLowerInvariant();
                    var url = $"{packageBaseAddress}/{id}/{version}/{id}.{version}.nupkg";

                    return PhpValue.FromClass(new ThemeResult(p)
                    {
                        download_link = url
                    });
                }
                else
                {
                    return PhpValue.Null;
                }
            }

            // default, fallback to WP API:

            return PhpValue.False;
        }

        PhpValue InstallSourceSelection(string source)
        {
            var package = _packages.InstallPackage(source, out var nuspec);
            if (package != null)
            {
                // get content of the plugin to be copied to wp-content/[plugins|themes]/
                var contentsource = Path.Combine(source, "contentFiles/any/netcoreapp2.0/wordpress/wp-content");
                Debug.Assert(Directory.Exists(contentsource));

                foreach (var type in new[] { "plugins", "themes" })
                {
                    var fullcontentsource = Path.Combine(contentsource, type, nuspec.GetId());
                    if (Directory.Exists(fullcontentsource))
                    {
                        return fullcontentsource + "/";    // ends with / because wp expects so
                    }
                }
            }

            // fallback to regular WP plugin, results in error
            return source; // PhpValue.FromClass( new WP_Error(...) );
        }

        PhpValue PostInstall(bool response, PhpArray hook_extra, PhpArray result)
        {
            if (hook_extra != null && result != null && hook_extra["type"] == "plugin" && hook_extra["action"] == "install")
            {
                _packages.LoadPackage(result["destination_name"].AsString());
            }

            return result;
        }

        void DeletePlugin(string plugin_file)
        {
            _packages.UninstallPackage(PackagesHelper.PluginFileToPluginId(plugin_file));
        }

        void SwitchTheme(string new_name, WP_Theme new_theme, WP_Theme old_theme = null)
        {
            Trace.WriteLine($"Switching theme from {old_theme.get_stylesheet()} to {new_theme.get_stylesheet()} ...");

            _packages.DeactivatePackage(old_theme.get_stylesheet().ToString());
            _packages.ActivatePackage(new_theme.get_stylesheet().ToString());
        }

        void IWpPlugin.Configure(WpApp app)
        {
            // postpone admin actions
            app.AddFilter("admin_init", new Action(() =>
            {
                // plugins_api
                app.AddFilter("plugins_api", new Func<PhpValue, string, object, PhpValue>(PluginsApi), accepted_args: 3);
                app.AddFilter("themes_api", new Func<PhpValue, string, object, PhpValue>(ThemesApi), accepted_args: 3);
                app.AddFilter("delete_plugin", new Action<string>(DeletePlugin));
                app.AddFilter("upgrader_source_selection", new Func<string, PhpValue>(InstallSourceSelection), priority: 0);
                app.AddFilter("upgrader_post_install", new Func<bool, PhpArray, PhpArray, PhpValue>(PostInstall), priority: 0, accepted_args: 3);
                app.AddFilter("activate_plugin", new Action<string>(plugin_file => _packages.ActivatePackage(PackagesHelper.PluginFileToPluginId(plugin_file))));
                app.AddFilter("deactivate_plugin", new Action<string>(plugin_file => _packages.DeactivatePackage(PackagesHelper.PluginFileToPluginId(plugin_file))));
                app.AddFilter("switch_theme", new Action<string, WP_Theme, WP_Theme>(SwitchTheme), accepted_args: 3);
                app.AddFilter("install_plugins_tabs", new Func<PhpArray, PhpArray>(tabs => // remove unsupported tabs
                {
                    //tabs.Remove(new IntStringKey("upload"));
                    tabs.Remove(new IntStringKey("popular"));
                    tabs.Remove(new IntStringKey("recommended"));
                    tabs.Remove(new IntStringKey("favorites"));
                    tabs["recommended"] = "Premium";
                    return tabs;
                }));
                app.AddFilter("user_has_cap", new Func<PhpArray, PhpArray, PhpArray, PhpArray>((allcaps, cap, args) =>
                {
                    // remove 'update_core'
                    allcaps["update_core"] = false;
                    //
                    return allcaps;
                }), accepted_args: 3);
                // defined in PeachPied.WordPress.Sdk: // app.Context.DefineConstant("FS_METHOD", "direct"); // overwrite how installing plugins is handled, skips the fs check
            }));
        }
    }
}