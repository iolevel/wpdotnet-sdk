using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Peachpied.WordPress.NuGetPlugins.Protocol
{
    class RawVersionInfo : VersionInfo
    {
        [JsonProperty(JsonProperties.Published)]
        public DateTimeOffset? Published { get; }

        public RawVersionInfo(NuGetVersion version, string downloadCount, DateTimeOffset? published = null)
            : base(version, downloadCount)
        {
            this.Published = published;
        }
    }
}
