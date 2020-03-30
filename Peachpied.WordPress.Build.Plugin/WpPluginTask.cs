using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Peachpied.WordPress.Build.Plugin
{
    public class WpPluginTask : Task
    {
        [Required]
        public string ProjectPath { get; set; }

        [Required]
        public string WpSlug { get; set; }

        [Required]
        public string WpContentTarget { get; set; }

        [Output]
        public string Version { get; set; }

        [Output]
        public string PackageProjectUrl { get; set; }

        [Output]
        public string PackageTags { get; set; }

        [Output]
        public string Authors { get; set; }

        [Output]
        public string Title { get; set; }

        [Output]
        public string Description { get; set; }

        [Output]
        public string PackageIconUrl { get; set; }

        //[Output]
        //public List<ITaskItem> MarketplaceAssets { get; } = new List<ITaskItem>();

        bool IsPlugin => WpContentTarget.EndsWith("plugins", StringComparison.Ordinal); // plugins, mu-plugins
        bool IsTheme => WpContentTarget == "themes";

        /// <summary>Matches wp metadata in a .php file; Tag:Value</summary>
        static readonly Regex s_regex_meta = new Regex(@"^[ \t\/*#@]*(?<Tag>[a-zA-Z ]+):[ \t]*(?<Value>.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public override bool Execute()
        {

            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

                    if (meta.ContainsKey("version") && meta.ContainsKey("plugin name")) break;
                }
            }

            // sections
            foreach (var f in new[] { "metadata.txt", "readme.txt" })
            {
                var fname = Path.Combine(ProjectPath, f);
                if (File.Exists(fname))
                {
                    string section = null;
                    foreach (var line in File.ReadLines(fname))
                    {
                        Match m;
                        if (section == null && !meta.ContainsKey("title") && (m = Regex.Match(line, @"^==+\s+(?<Name>[a-zA-Z\(\) ]+)==+")).Success)
                        {
                            meta["title"] = m.Groups["Name"].Value.Trim();
                        }
                        else if (section == null && (m = Regex.Match(line, @"^(?<Tag>[a-zA-Z ]+):[ \t]*(?<Value>.+)$")).Success)
                        {
                            meta[m.Groups["Tag"].Value.Trim()] = m.Groups["Value"].Value.Trim();
                            Log.LogMessage(m.Value, MessageImportance.High);
                        }
                        else if ((m = Regex.Match(line, @"^==\s+(?<Name>[a-zA-Z ]+)==$")).Success)
                        {
                            section = m.Groups["Name"].Value.Trim();
                            sections[section] = "";
                        }
                        else if (section != null)
                        {
                            sections[section] += line + "\n";
                        }
                    }
                }
            }

            if (IsPlugin)
            {
                // icon
                foreach (var urlformat in new[] { "https://ps.w.org/{0}/assets/icon-256x256.png", "https://ps.w.org/{0}/assets/icon-256x256.jpg", "https://ps.w.org/{0}/assets/icon.svg", "https://ps.w.org/{0}/assets/icon-128x128.png" })
                {
                    var url = string.Format(urlformat, WpSlug);
                    if (CheckUrl(url))
                    {
                        PackageIconUrl = url;
                        break;
                    }
                }
            }

            if (IsTheme)
            {
                // screenshot
                var screenshot = Directory.GetFiles(ProjectPath, "screenshot.*").FirstOrDefault();
                if (screenshot != null)
                {
                    //PackageIconUrl = FeedUrl + "assets/wptheme/" + WpSlug + Path.GetExtension(screenshot);
                }
            }

            //// readme
            //var dict = Directory.GetFiles(ProjectPath)  // [.extension, readmepath ]
            //    .Where(x => Path.GetFileNameWithoutExtension(x).Equals("readme", StringComparison.OrdinalIgnoreCase))
            //    .ToDictionary(Path.GetExtension, StringComparer.OrdinalIgnoreCase);

            //string readme;
            //if (dict.TryGetValue(".txt", out readme) || dict.TryGetValue(".md", out readme)) // read .txt if possible
            //{
            //    //var remotepath = "assets/" + MarketplaceTarget + "/" + Slug + Path.GetExtension(readme).ToLower();
            //    //Upload(readme, remotepath);
            //    MarketplaceAssets.Add(new TaskItem(readme));
            //}

            // properties
            string s;
            Version = (meta.TryGetValue("version", out s) || meta.TryGetValue("Stable tag", out s)) ? s : null;
            PackageProjectUrl = (meta.TryGetValue("Plugin URI", out s) || meta.TryGetValue("website", out s) || meta.TryGetValue("theme uri", out s) || meta.TryGetValue("url", out s)) ? s : null;
            PackageTags = (meta.TryGetValue("tags", out s) || sections.TryGetValue("tags", out s)) ? s.Replace(", ", ",") : null;
            Authors = (meta.TryGetValue("Author", out s) || meta.TryGetValue("contributors", out s)) ? s : null;
            Title = (meta.TryGetValue("Plugin Name", out s) || meta.TryGetValue("title", out s)) ? s : null;
            Description = (meta.TryGetValue("Description", out s) || sections.TryGetValue("Description", out s)) ? s : null;

            // done
            return true;
        }

        static bool CheckUrl(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (200 == (int)response.StatusCode)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }


    }
}
