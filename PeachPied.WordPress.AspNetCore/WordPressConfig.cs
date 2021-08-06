using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore
{
    /// <summary>
    /// WordPress configuration.
    /// The configuration is loaded into WordPress before every request.
    /// </summary>
    public class WordPressConfig
    {
        // DATABASE:

        /// <summary>MySQL database name.</summary>
        public string DbName { get; set; } = "wordpress";

        /// <summary>MySQL database user name.</summary>
        public string DbUser { get; set; } = "root";

        /// <summary>MySQL database password.</summary>
        public string DbPassword { get; set; }

        /// <summary>MySQL host.</summary>
        public string DbHost { get; set; } = "localhost";

        //

        /// <summary>WordPress Database Table prefix.</summary>
        /// <remarks>
        /// You can have multiple installations in one database if you give each
        /// a unique prefix. Only numbers, letters, and underscores please!
        /// </remarks>
        public string DbTablePrefix { get; set; } = "wp_";

        /// <summary>
        /// Controls the <c>WP_SITEURL</c> configuration constant.
        /// The value defined is the address where your WordPress core files reside. It should include the http:// part too. Do not put a slash “/” at the end.
        /// </summary>
        public string SiteUrl
        {
            get => _siteUrl;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (!value.StartsWith("http"))
                        throw new ArgumentException("SiteUrl must start with http:// or https://.");
                    if (value.EndsWith('/'))
                        throw new ArgumentException("SiteUrl must not have trailing slash.");
                }

                _siteUrl = value;
            }
        }

        private string _siteUrl;

        /// <summary>
        /// Controls the <c>WP_HOME</c> configuration constant.
        /// Represents the address you want people to type in their browser to reach the WordPress blog.
        /// It should include the http:// part and should not have a slash “/” at the end.
        /// Adding this in can reduce the number of database calls when loading the site.
        /// </summary>
        public string HomeUrl
        {
            get => _homeUrl;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (!value.StartsWith("http"))
                        throw new ArgumentException("HomeUrl must start with http:// or https://.");
                    if (value.EndsWith('/'))
                        throw new ArgumentException("HomeUrl must not have trailing slash.");
                }

                _homeUrl = value;
            }
        }

        private string _homeUrl;

        // SALT:

        /// <summary>
        /// Authentication Unique Keys and Salts.
        /// </summary>
        /// <remarks>
        /// Change these to different unique phrases!
        /// You can generate these using the WordPress.org secret-key service - https://api.wordpress.org/secret-key/1.1/salt/
        /// You can change these at any point in time to invalidate all existing cookies. This will force all users to have to log in again.
        /// </remarks>
        public class SaltData
        {
            /// <summary></summary>
            public string AUTH_KEY { get; set; } = "*Sr748b66z3R+(v%1z;|SCtBZz/cEvo1)mo|F&EO>5a^1aF6@C9^KIzG&MD?=Zmt";
            /// <summary></summary>
            public string SECURE_AUTH_KEY { get; set; } = "P]-!;,$G96Gf`8pO-1e;t%Y1hYfU{}lRdhgl#h./C`_gxJsd^`3[$yoz!pe4bX(U";
            /// <summary></summary>
            public string LOGGED_IN_KEY { get; set; } = "E$0Y`&8,IgAME5<OTD:*]x}$wEhEemY|2PVzQ!!96:F&0S{gu|S|TZ!} ^-l}xgh";
            /// <summary></summary>
            public string NONCE_KEY { get; set; } = "0)ET<zQ RlA$Gb5R*>UO]zKpgF-CxT?J0u8<m?;HhpAm!aY @qWTNI{A]>$Jow#>";
            /// <summary></summary>
            public string AUTH_SALT { get; set; } = "!|gQ:L<;]+F:mt<wV)]n &,7iv{D5dG+kLi<S$}Vp-*@Ev.+-P4p|lRQOnh]2jKV";
            /// <summary></summary>
            public string SECURE_AUTH_SALT { get; set; } = "wlk)xBD7EC0|zJCs&`&oK#3<O2THx,{=He|^]+PFwVN%{m38bK.||-]@-1:4,7}f";
            /// <summary></summary>
            public string LOGGED_IN_SALT { get; set; } = "g}oD ]M2)SMa^zPx(}~6RPXP{7{!|`(IQCnY.2xQHv4HxV9f8;CoH~+]M01w/o(y";
            /// <summary></summary>
            public string NONCE_SALT { get; set; } = "SVVq/47*B)T_&aFj.tN^c9U =uI>7QS+WSuR[leI+PpDbJ_K_fu06Qyrq~5s{3=-";
        }

        /// <summary>
        /// Authentication Unique Keys and Salts.
        /// </summary>
        public SaltData SALT { get; private set; } = new SaltData();

        /// <summary>
        /// Additional PHP constants to be set before each request starts.
        /// </summary>
        public Dictionary<string, string> Constants { get; set; }

        // 

        /// <summary>
        /// Whether to enable or disable response caching.
        /// Enabled by default.
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// Overrides <c>WP_DEBUG</c> constant.
        /// </summary>
        public bool Debug { get; set; } = false;

        /// <summary>
        /// Enumeration of assembly names with compiled PHP plugins, themes or other additions.
        /// These assemblies will be loaded, treated as PHP assemblies containing script files and will be loaded into the entire application context.
        /// </summary>
        public List<string> LegacyPluginAssemblies { get; set; }

        /// <summary>
        /// Collection of .NET plugins implemented as <see cref="IWpPlugin"/>.
        /// </summary>
        public WpPluginContainer PluginContainer { get; } = new WpPluginContainer();

        /// <summary>
        /// MEF composition container.
        /// Will be used to import <see cref="IWpPluginProvider"/> parts.
        /// </summary>
        public ContainerConfiguration CompositionContainers { get; } = new ContainerConfiguration();
    }
}
