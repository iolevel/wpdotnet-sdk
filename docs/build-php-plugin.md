# Build a PHP plugin

Single plugin for WordPress is defined as a class library project which is then referenced by the application. Application can have references to one or more plugins.

## Plugin source files

Sources of the plugin complies with the regular WordPress codex. See [Writing a Plugin](https://codex.wordpress.org/Writing_a_Plugin) for more details. Put the plugin source files in an empty folder.

Following is a file structure for the basic WordPress [*Hello Dolly* plugin](https://wordpress.org/plugins/hello-dolly/).

```shell
┬ MyHelloDollyPlugin
└┬ hello.php
 └ readme.txt
```

Name or location of the folder are both not relevant.

Note, the plugin must contain the standard plugin metadata. The main source file needs to specify following minimal metadata at the top of the file:

> *hello.php:*
```php
<?php
/*
Plugin Name: Hello Dolly
Version: 1.7.2
*/

```

## Plugin project file

In order to compile the plugin, create a project file in the plugin folder.

```shell
┬ MyHelloDollyPlugin
└┬ hello.php
 ├ MyHelloDollyPlugin.msbuildproj  <---
 └ readme.txt
```

By default, the name of the project file represents the plugin's *id*. *Id*, or *slug id* is the identifier of the plugin within WordPress.

Project file is an XML file with the following content:

> *MyHelloDollyPlugin.msbuildproj:*
```xml
<Project Sdk="PeachPied.WordPress.Build.Plugin/5.7.0-preview6">

</Project>
```

For most cases, the project file does not specify anything else as all the properties are defined by default in the Sdk. In case a build property or a build item needs to be altered, add it to your project file.

Note the project file specifies a version after the slash, i.e. `"/5.7.0-preview6"`. This corresponds to the version of *PeachPied.WordPress.** packages which should be identical across all your application.

## Build the plugin

In order to build the plugin, run `dotnet build` command:

```shell
dotnet build
```

The process will build the plugin project. Eventual warnings will be outputed. Any error in the code will cause the build to fail.

> If the process is run for the first time, it will first download the project Sdk and dependency packages (it means it requires an Internet connection).

### Add plugin to the application

Assuming you have ASP.NET Core Application (called *app*) with WordPress (see [quick start](../overview/#quick-start)). Adding plugins to the application is equivalent to adding project references or package references.

Either add project reference to the plugin in Visual Studio IDE, or on command line, or edit the application's project file:

> *app.csproj:*
```xml
<ItemGroup>
  <ProjectReference Include="../MyHelloDollyPlugin/MyHelloDollyPlugin.msbuildproj" />
</ItemGroup>
```

> Note, the plugin is referenced by the application and it still needs to be activated in WordPress dashboard.

## Package the plugin

Plugin can be packed into a standard NuGet package. Run `dotnet pack` command to create NuGet package:

```shell
dotnet pack
```

Package (*.nupkg*) can be pushed into a NuGet feed. It is recommended to push packages to private feeds, or at least pack packages with a unique prefixed package identifier (i.e. *MyName.MyWpPlugins.MyHelloDollyPlugin*).

### Add package reference to the application

Once plugin is packed and available on a NuGet feed, it can be referenced as a package reference. Either add package reference to the plugin in Visual Studio IDE, or on command line, or edit the application's project file:

```xml
<ItemGroup>
  <PackageReference Include="MyHelloDollyPlugin" Version="1.7.2" />
</ItemGroup>
```

## Debugging the plugin

> not yet documented