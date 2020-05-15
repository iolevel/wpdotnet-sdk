using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace PeachPied.WordPress.Build.Plugin
{
    /// <summary>
    /// Task parses wp plugin/theme metadata and sets corresponsing properties.
    /// </summary>
    public class WpPluginTask : Task
    {
        /// <summary>
        /// Directory with the project, expecting to contain the plugin metadat files.
        /// </summary>
        [Required]
        public string ProjectPath { get; set; }

        /// <summary>
        /// WP slug id. Used to resolve eventual icon URL.
        /// </summary>
        [Required]
        public string WpSlug { get; set; }

        /// <summary>
        /// The wp-content sub-dir.
        /// </summary>
        [Required]
        public string WpContentTarget { get; set; }

        /// <summary>
        /// Specified VersionSuffix if any.
        /// </summary>
        public string VersionSuffix { get; set; }

        /// <summary>
        /// Version from metadata.
        /// </summary>
        [Output]
        public string Version { get; set; }

        /// <summary>
        /// Project URL from metadata.
        /// </summary>
        [Output]
        public string PackageProjectUrl { get; set; }

        /// <summary>
        /// Tags from metadata.
        /// </summary>
        [Output]
        public string PackageTags { get; set; }

        /// <summary>
        /// Authors from metadata.
        /// </summary>
        [Output]
        public string Authors { get; set; }

        /// <summary>
        /// Title from metadata.
        /// </summary>
        [Output]
        public string Title { get; set; }

        /// <summary>
        /// Description from metadata.
        /// </summary>
        [Output]
        public string Description { get; set; }

        //[Output]
        //public List<ITaskItem> MarketplaceAssets { get; } = new List<ITaskItem>();

        bool IsPlugin => WpContentTarget.EndsWith("plugins", StringComparison.Ordinal); // plugins, mu-plugins

        bool IsTheme => WpContentTarget == "themes";

        /// <summary>Matches wp metadata in a .php file; Tag:Value</summary>
        static readonly Regex s_regex_meta = new Regex(@"^[ \t\/*#@]*(?<Tag>[a-zA-Z ]+):[ \t]*(?<Value>.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Removes spaces between comma separated list of tags.
        /// Lowercases the string.
        /// </summary>
        public static string NormalizeTagList(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return string.Empty;
            }

            // replace all ", " with comma ","
            // trim whitespaces and delimiters
            var regex_comma = new Regex(@"\s*,\s+", RegexOptions.CultureInvariant);
            return regex_comma.Replace(tags, ",").Trim(' ', '\t', ',', ';').ToLowerInvariant();
        }

        /// <summary>
        /// Parses readme.txt file format providing its meta tags, sections, and resolved short description.
        /// </summary>
        public static bool TryParseReadmeTxt(IEnumerable<string> lines, Dictionary<string, string> meta, Dictionary<string, string> sections, out string description)
        {
            // NOTE: the file can have sections in TXT format or Markdown format

            const string NewLine = "\n";

            string title = null;   // first section name
            string shortdescription = null;
            string metatag = null; // current meta tag
            string section = null; // current section
            Regex regex_section = null; // regexp to match proper section name, determined on first line

            // alternative section name regex
            var altsection = new Regex(@"^\s*##+\s*(?<Name>[^=]+)##+\s*$", RegexOptions.CultureInvariant);

            //
            foreach (var line in lines)
            {
                if (regex_section == null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // first line:
                    if (line.StartsWith("# "))
                    {
                        // ## Name
                        regex_section = new Regex(@"^#+\s+(?<Name>.+)$", RegexOptions.CultureInvariant); // markdown headers
                    }
                    else if (line.StartsWith("=="))
                    {
                        // at least "={2,}"
                        regex_section = new Regex(@"^\s*==+\s*(?<Name>[^=]+)==+\s*$", RegexOptions.CultureInvariant);
                    }
                    else
                    {
                        // unknown line,
                        // some themes and plugins prefixes the initial section with some text
                        continue;
                    }
                }

                //

                Match m;
                if (((m = regex_section.Match(line)).Success ||
                     (m = altsection.Match(line)).Success)
                    && !line.StartsWith("###")) // section, but not markdown h3, h4, ...
                {
                    var value = m.Groups["Name"].Value.Trim(' ', '\t', '#');
                    if (title == null)
                    {
                        meta["title"] = title = value;
                    }
                    else
                    {
                        section = value;
                        sections[section] = "";
                    }
                }
                else if (section == null)
                {
                    // meta information:
                    if ((m = Regex.Match(line, @"^\**(?<Tag>[a-zA-Z ]+):[ \t\*]*(?<Value>.*)$")).Success)
                    {
                        metatag = m.Groups["Tag"].Value.Trim();

                        var value = m.Groups["Value"].Value.Trim();
                        if (value.Length != 0)
                        {
                            meta[metatag] = value;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        metatag = null;
                    }
                    else
                    {
                        if (metatag != null)
                        {
                            // append to the current meta
                            meta.TryGetValue(metatag, out var value);
                            meta[metatag] = $"{value} {line}";
                        }
                        else
                        {
                            // short description follows meta tags
                            shortdescription += (shortdescription != null ? NewLine : null) + line;
                        }
                    }
                }
                else
                {
                    sections[section] += line + "\n";
                }
            }

            // resolve description
            // short description
            if (meta.TryGetValue("Description", out var s))
            {
                // prefer short description from metadata
                shortdescription = s.Trim();
            }

            if (
                (string.IsNullOrWhiteSpace(shortdescription) ||  // short description is missing 
                 shortdescription.StartsWith("Copyright"))       // or it's actually a short copyright :/
                && sections.TryGetValue("Description", out s))
            {
                // short description not read from meta,
                // get it from full description section
                var emptyline = new Regex(@"^\s+$", RegexOptions.CultureInvariant | RegexOptions.Multiline);

                // trim leading and trailing decoration characters
                s = s.Trim('-', ' ', '\t', '#');

                // normalize whitespaces,
                // take first paragraph
                s = s.Replace("\r\n", NewLine);
                s = emptyline.Replace(s, "");

                var nl = s.IndexOf(NewLine + NewLine, StringComparison.Ordinal);
                if (nl > 0)
                    s = s.Remove(nl);

                //
                shortdescription = s;
            }

            //
            description = shortdescription;
            return title != null;
        }

        /// <summary>
        /// Collects metadata.
        /// </summary>
        public override bool Execute()
        {
            var meta = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var sections = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            if (IsPlugin)
            {
                // meta
                foreach (var fname in Directory.GetFiles(ProjectPath, "*.php")) // php files in the directory
                {
                    foreach (var line in File.ReadLines(fname).Take(26)) // up to 26 lines
                    {
                        var m = s_regex_meta.Match(line);
                        if (m.Success)
                        {
                            meta[m.Groups["Tag"].Value.Trim()] = m.Groups["Value"].Value.Trim();
                            Log.LogMessage(m.Value, MessageImportance.High);
                        }
                    }

                    if (meta.ContainsKey("version") && meta.ContainsKey("plugin name"))
                    {
                        if (meta.TryGetValue("description", out var shortdescription))
                        {
                            Description = shortdescription;
                        }

                        break;
                    }
                }
            }

            // sections
            foreach (var f in new[] { "metadata.txt", "readme.txt" })
            {
                var fname = Path.Combine(ProjectPath, f);
                if (!File.Exists(fname))
                {
                    continue;
                }

                if (TryParseReadmeTxt(File.ReadLines(fname), meta, sections, out var shortdesc))
                {
                    if (string.IsNullOrEmpty(Description) && !string.IsNullOrEmpty(shortdesc))
                    {
                        Description = shortdesc;
                    }
                }
            }

            if (IsTheme)
            {
                // screenshot
                var screenshot = Directory.GetFiles(ProjectPath, "screenshot.*").FirstOrDefault();
                if (screenshot != null)
                {
                    //PackageIconUrl = "assets/icon" + Path.GetExtension(screenshot);
                }
            }

            // properties
            string s;
            Version = ConstructVersion((meta.TryGetValue("version", out s) || meta.TryGetValue("Stable tag", out s)) ? s : null, VersionSuffix);
            PackageProjectUrl = (meta.TryGetValue("Plugin URI", out s) || meta.TryGetValue("website", out s) || meta.TryGetValue("theme uri", out s) || meta.TryGetValue("url", out s)) ? s : null;
            PackageTags = (meta.TryGetValue("tags", out s) || sections.TryGetValue("tags", out s)) ? NormalizeTagList(s) : null;
            Authors = (meta.TryGetValue("Author", out s) || meta.TryGetValue("contributors", out s)) ? s : null;
            Title = string.IsNullOrWhiteSpace(Title) ? (meta.TryGetValue("Plugin Name", out s) || meta.TryGetValue("title", out s)) ? s : null : Title;

            if (string.IsNullOrWhiteSpace(Description))
            {
                // description cannot be empty
                Description = Title;
            }

            // done
            return true;
        }

        static string ConstructVersion(string metaversion, string versionsuffix)
        {
            if (string.IsNullOrEmpty(metaversion))
            {
                return null;
            }

            // vX.Y.Z-PreRelease
            if (metaversion[0] == 'v') metaversion = metaversion.Substring(1);

            var dash = metaversion.IndexOf('-');
            var prefix = dash < 0 ? metaversion : metaversion.Remove(dash);
            var suffix = string.IsNullOrEmpty(versionsuffix)
                ? (dash < 0 ? "" : metaversion.Substring(dash + 1))
                : versionsuffix;

            return string.IsNullOrEmpty(suffix) ? prefix : $"{prefix}-{suffix}";
        }
    }
}
