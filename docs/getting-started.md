---
title: Getting Started
---
To get started with a basic WpDotNet app, follow this tutorial.

## Prerequisites

- .NET SDK 6.0, or newer. ([dotnet.microsoft.com](https://dotnet.microsoft.com/download))
- MySQL Server ([dev.mysql.com](https://dev.mysql.com/downloads/mysql/) or [docker](https://hub.docker.com/_/mysql))

Make sure you have valid credentials to your MySQL server and you have created a database in it. The following quick start expects a database named `"wordpress"`. Database charset `"UTF-8"` is recommended.

## Quick Start

Open or create an ASP NET Core application, version 6.0 or newer.

```shell
dotnet new web
```

Add a package reference to [`"Peachpied.WordPress.AspNetCore"`](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/) (note it is a **pre-release** package):

```shell
dotnet add package PeachPied.WordPress.AspNetCore --version 6.5.4-rc-020
```
!!! tip "Always have access to the latest build!"
    If you want the absolute latest release build with the most recent implementations and bug fixes, please consider becoming a [Patron](patron.md) for this and many other benefits.


Add the WordPress services and set up your database connection (here or in `appsettings.json`):

```C#
builder.Services.AddWordPress(options =>
{
    options.DbHost = "localhost";
    options.DbName = "wordpress";
    // ...
});
```

> Note: the recommended approach is to place the configuration within the `appsettings.json` configuration file. See [configuration](configuration.md) for more details.

Add the WordPress middleware within your request pipeline:

```C#
// ...
app.UseWordPress();
// ...
```

## Sample App

The sources of a demo WordPress application are available at [github.com/iolevel/peachpie-wordpress](https://github.com/iolevel/peachpie-wordpress).

## Dashboard

Besides regular WordPress dashboard pages, WpDotNet adds an informational panel on the Dashboard Home page, within the *At a Glance* widget.

![WpDotNet At Glance](img/wp-dashboard-glance.png)

The panel provides information about the current .NET runtime version, consumed memory, or total CPU time spent in the whole application. Note that the values are reset if the process is restarted.

## Differences

The main differences between regular WordPress running on PHP and WpDotNet running on .NET are:

- The .NET application and all its [plugins/themes](plugins-php.md) need to be compiled before running. Plugins and themes cannot be added after building the project.
- The WordPress configuration is not set in `wp-config.php` file anymore. WpDotNet uses ASP.NET Core configuration providers like `appsettings.json`. See [configuration](configuration.md).
- There is literally no `php` intepreter; all the PHP standard functions and extensions are re-implemented in .NET and their behavior may differ, i.e. break some functionality. In such case please let us know.

## Notes

- Permalinks are implicitly enabled through the URL rewriting feature.
- WordPress debugging is implicitly enabled when running in a *Development* environment (debugging in your IDE).
- When running on Azure Web App with _MySql in App_ enabled, the database connection is automatically configured.
- Response caching and response compression are enabled by default when the user is not logged in.
- Most of the original `.php` files are not present on the file system and cannot be edited.

## Next Steps

- [Tutorial: Build ASP.NET Core app with WordPress](tutorials/aspnetcore-wordpress.md): Step-by-step creating WordPress app in Visual Studio.
- [Add WordPress Plugins/Themes](plugins-php.md): Extend WpDotNet with WordPress/PHP plugins and themes from `.php` sources.
- [Public NuGet Release](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/): Free WpDotNet release versions.

