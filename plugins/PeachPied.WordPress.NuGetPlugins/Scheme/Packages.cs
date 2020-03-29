using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Peachpied.WordPress.NuGetPlugins.Scheme
{
    [JsonObject]
    sealed class Packages
    {
        public IEnumerable<InstalledPackage> installed { get; set; }

        public void Add(InstalledPackage package)
        {
            Remove(package.pluginId);

            installed = new List<InstalledPackage>(installed)
            {
                package
            };
        }

        public void Remove(string packageId)
        {
            this.installed = installed.Where(p => p.pluginId != packageId);
        }

        public void SetActivate(string packageId, bool active)
        {
            foreach (var p in installed)
            {
                if (p.pluginId == packageId)
                {
                    p.active = active;
                    break;
                }
            }
        }
    }
}
