using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Peachpied.WordPress.AspNetCore.Marketplace.Scheme
{
    [JsonObject]
    sealed class InstalledPackage
    {
        public string pluginId { get; set; }
        public string version { get; set; }
        public bool active { get; set; }
    }
}
