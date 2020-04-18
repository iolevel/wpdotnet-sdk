using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Pchp.Core;

namespace Peachpied.WordPress.NuGetPlugins
{
    sealed class PackagesHelper
    {
        /*
         * Plugins are installed into a bin folder of our choice
         * Since we frequently change runtime and API itself, plugins for older version are not compatible with newer version and so on:
         * !! folder with plugins changes with every update !!
         */

        public static string InformationalVersion = typeof(WP).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        static string PackagesPath
        {
            get
            {
                var appass = Assembly.GetEntryAssembly();
                var wpversion = InformationalVersion;

                // TODO: configurable "packages"

                return Path.Combine(Path.GetDirectoryName(appass.Location), "packages", wpversion);
            }
        }

        static string PackagesJsonPath => Path.Combine(PackagesPath, "packages.json");

        public static string PluginFileToPluginId(string plugin_file)
        {
            // plugin_file is relative to the plugins directory, one of following:
            // - slug/pluginfile.php
            // - slug.php

            var dirname = Path.GetDirectoryName(plugin_file);
            var slug = string.IsNullOrEmpty(dirname) ? Path.GetFileName(plugin_file) : dirname;

            return slug;
        }

        static Scheme.Packages GetPackagesJson()
        {
            var path = PackagesJsonPath;
            var json = File.Exists(path)
                ? JsonConvert.DeserializeObject<Scheme.Packages>(File.ReadAllText(PackagesJsonPath))
                : new Scheme.Packages();

            if (json.installed == null)
            {
                json.installed = Array.Empty<Scheme.InstalledPackage>();
            }

            return json;
        }

        static void SavePackagesJson(Scheme.Packages json)
        {
            Directory.CreateDirectory(PackagesPath);
            File.WriteAllText(PackagesJsonPath, JsonConvert.SerializeObject(json));
        }

        static Scheme.Packages UpdatePackagesJson(Action<Scheme.Packages> action)
        {
            var json = GetPackagesJson();
            action(json);
            SavePackagesJson(json);

            return json;
        }

        public void LoadPackages()
        {
            var packages = GetPackagesJson();
            foreach (var p in packages.installed)
            {
                var packageroot = Path.Combine(PackagesPath, p.pluginId);

                if (!Directory.Exists(packageroot)) continue;

                // load plugin:
                if (p.active)
                {
                    LoadPackage(p);
                }

                // delete old versions:
                foreach (var d in Directory.GetDirectories(packageroot))
                {
                    if (Path.GetFileName(d) != p.version)
                    {
                        try { Directory.Delete(d, true); } catch { }
                    }
                }
            }
        }

        internal bool LoadPackage(string packageId)
        {
            var package = GetPackagesJson().installed.FirstOrDefault(p => p.pluginId == packageId);

            return LoadPackage(package);
        }

        internal bool LoadPackage(Scheme.InstalledPackage package)
        {
            bool loaded = false;

            if (package != null)
            {
                var packagepath = Path.Combine(PackagesPath, package.pluginId, package.version);

                if (Directory.Exists(packagepath))
                {
                    foreach (var fname in Directory.GetFiles(packagepath, "*.dll"))
                    {
                        try
                        {
                            var ass = Assembly.LoadFrom(fname);
                            foreach (var refass in ass.GetReferencedAssemblies())
                            {
                                if (refass.Name == "PeachPied.WordPress.Standard" && refass.Version != typeof(PeachPied.WordPress.Standard.WpApp).Assembly.GetName().Version)
                                {
                                    throw new FileLoadException($"The plugin '{package.pluginId}' is built for a different WpDotNet SDK version and won't be loaded.");
                                }
                            }

                            Context.AddScriptReference(ass);
                            loaded = true;
                        }
                        catch
                        {
                            // TOOD: log
                        }
                    }
                }
            }

            return loaded;
        }

        public void ActivatePackage(string packageId)
        {
            var json = UpdatePackagesJson(_json => _json.SetActivate(packageId, true));
            var package = json.installed.FirstOrDefault(p => p.pluginId == packageId);

            LoadPackage(package);
        }

        public void DeactivatePackage(string packageId)
        {
            UpdatePackagesJson(json => json.SetActivate(packageId, false));

            // deactivate loaded scripts
            if (Context.TryGetScriptsInDirectory("", Pchp.Core.Utilities.CurrentPlatform.NormalizeSlashes("wp-content/plugins/" + packageId), out var scripts))
            {
                foreach (var s in scripts)
                {
                    Context.DeclareScript(s.Path, new Context.MainDelegate((_ctx, _locals, _this, _self) => PhpValue.False)); // TODO: throw?
                }
            }
        }

        /// <summary>
        /// Installs the (unzipped) nuget package into <see cref="PackagesPath"/> directory.
        /// </summary>
        /// <param name="nugetContentPath">Content of the unzipped nuget.</param>
        /// <param name="nuspecreader">Outputs nuspec file.</param>
        /// <returns>Whether the installation was successful.</returns>
        public Scheme.InstalledPackage InstallPackage(string nugetContentPath, out INuspecCoreReader nuspecreader)
        {
            var nuspecs = Directory.GetFiles(nugetContentPath, "*.nuspec");
            if (nuspecs.Length == 1)
            {
                var nuspec = new NuspecCoreReader(XDocument.Load(nuspecs[0]));

                // TODO: restore dependencies

                // copy lib to packages
                var dllsource = Path.Combine(nugetContentPath, "lib/netstandard2.1");
                var dlltarget = Path.GetFullPath(Path.Combine(PackagesPath, nuspec.GetId(), nuspec.GetVersion().ToNormalizedString()));

                Directory.CreateDirectory(dlltarget);

                foreach (var fpath in Directory.GetFiles(dllsource))
                {
                    var filetarget = Path.Combine(dlltarget, Path.GetFileName(fpath));
                    if (!File.Exists(filetarget))
                    {
                        File.Move(fpath, filetarget);
                    }
                }

                // TODO: try to delete old versions of the package

                //
                var package = new Scheme.InstalledPackage
                {
                    pluginId = nuspec.GetId(),
                    version = nuspec.GetVersion().ToNormalizedString(),
                    active = false
                };

                // add packageId to packages.json
                UpdatePackagesJson(json =>
                {
                    json.Add(package);
                });

                //
                nuspecreader = nuspec;
                return package;
            }
            else
            {
                nuspecreader = null;
                return null;
            }
        }

        public void UninstallPackage(string packageId)
        {
            UpdatePackagesJson(json => json.Remove(packageId));

            // remove packageId from packages.json
            DeactivatePackage(packageId);

            // delete versions:
            var packagePath = Path.Combine(PackagesPath, packageId);
            if (Directory.Exists(packagePath))
            {
                // delete dir packages/slug/
                foreach (var vdir in Directory.GetDirectories(packagePath))
                {
                    try { Directory.Delete(vdir, true); } catch { }
                }

                try { Directory.Delete(packagePath, true); } catch { }
            }
        }
    }
}
