# Plugins and Themes (.php)

Plugins and themes (either your own or plugins and themes downloaded from [WordPress directory](https://wordpress.org/plugins/)) need to be added during the development process and compiled. This article describes how and how it works.

**Sample project**: https://github.com/iolevel/peachpie-wordpress/tree/master/MyContent

## Project Structure

The following project structure respects the `/wp-content/` directory used by WordPress. Sources of the plugin complies with the regular WordPress codex. See [Writing a Plugin](https://codex.wordpress.org/Writing_a_Plugin) for more details. Put the plugin source files in `plugins` folder, and themes in `themes` folder (as you would do in WordPress).

Following is the file structure for the basic WordPress [*Hello Dolly* plugin](https://wordpress.org/plugins/hello-dolly/).

```shell
┬ MyContent
└  plugins
   └ hello.php
```

Name of the folder is not relevant.

Note, the plugin must contain the standard plugin metadata. The main source file needs to specify following minimal metadata at the top of the file:

> *hello.php:*
```php
<?php
/*
Plugin Name: Hello Dolly
Version: 1.7.2
*/

```

## Project File

In order to compile the plugin, create a project file in the plugin folder.

```shell
┬ MyContent
├  MyContent.msbuildproj  <---
└  plugins
   └ hello.php
```

Project file is an XML file with the following content:

> *MyContent.msbuildproj:*
```xml
<Project Sdk="PeachPied.WordPress.Build.Plugin/6.5.4-rc-020">
</Project>
```

For most cases, the project file does not specify anything else as all the properties are defined by default in the Sdk. In case a build property or a build item needs to be altered, add it to your project file.

Note the project file specifies a version after the slash, i.e. `"/6.5.4-rc-020"`. This corresponds to the version of *PeachPied.WordPress.** packages which should be identical across all your application.

## Build

In order to build plugins and themes, run `dotnet build` command:

```shell
dotnet build
```

The process will build the project. Eventual warnings will be outputed. Any error in the code will cause the build to fail.

> If the process is run for the first time, it will first download the project Sdk and dependency packages (it means it requires an Internet connection).

### Build Errors

Building the project may result in compile-time errors. It is very common for a WordPress plugin to contain an invalid code.

Errors need to be fixed. Sometimes whole files or directories may be excluded from the compilation since they may contain a dead code (usually tests or adapters for other plugins you don't use).

### Add Project Reference

Assuming you have ASP.NET Core Application (named *app*) with WordPress (see [quick start](index.md#quick-start)), add project reference to `MyContent.msbuildproj`.

Either add project reference to the plugin in Visual Studio IDE, or on command line, or edit the application's project file:

> *app.csproj:*
```xml
<ItemGroup>
  <ProjectReference Include="../MyContent/MyContent.msbuildproj" />
</ItemGroup>
```

> Note, the plugin still needs to be activated in WordPress dashboard once you compile and run the application.

## Debugging the plugin

