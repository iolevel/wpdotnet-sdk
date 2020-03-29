using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Peachpied.WordPress.NuGetPlugins.Protocol
{
    class RawPackageSearchMetadata : PackageSearchMetadata
    {
        [JsonProperty(JsonProperties.Versions)]
        public RawVersionInfo[] RawVersions { get; set; }
    }
}
