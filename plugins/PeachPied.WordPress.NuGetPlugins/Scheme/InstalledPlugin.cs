using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Peachpied.WordPress.NuGetPlugins.Scheme
{
    [JsonObject]
    sealed class InstalledPackage
    {
        public string pluginId { get; set; }
        public string version { get; set; }
        public bool active { get; set; }
    }
}
