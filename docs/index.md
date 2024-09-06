---
title: Overview
social:
  cards_layout_options:
    title: WpDotNet - WordPress on .NET - Overview
---

# Overview

[WpDotNet](https://www.wpdotnet.com/) is the unmodified WordPress, running compiled purely on .NET, provided as a NuGet package & ready to be used as a part of an ASP NET Core application. WpDotNet comes with additional components and features, making it easy to be used from C# and a .NET development environment in general.

The project does not require PHP to be installed, and is purely built on top of the .NET platform.

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

## Remarks

- Permalinks are implicitly enabled through the URL rewriting feature.
- WordPress debugging is implicitly enabled when running in a *Development* environment (debugging in your IDE).
- When running on Azure Web App with _MySql in App_ enabled, the database connection is automatically configured.
- Response caching and response compression are enabled by default when user is not logged in.
- Most of the original `.php` files are not present on the file system and cannot be edited.

## Related links

- https://www.wpdotnet.com/
- https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/   
[![NuGet](https://img.shields.io/nuget/v/PeachPied.WordPress.AspNetCore.svg)](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/)
