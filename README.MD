<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/2/20/WordPress_logo.svg/2000px-WordPress_logo.svg.png" width="400" style="display: block;margin-left: auto;margin-right: auto;"/>

**All of WordPress as a .NET Standard assembly, without PHP.**

  ![.NET Core](https://github.com/iolevel/wpdotnet-sdk/workflows/.NET%20Core/badge.svg)
  [![NuGet](https://img.shields.io/nuget/v/PeachPied.WordPress.AspNetCore.svg)](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/)
  [![Downloads](https://img.shields.io/nuget/dt/PeachPied.WordPress.AspNetCore.svg)](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/)
  ![Stars](https://img.shields.io/github/stars/iolevel/wpdotnet-sdk?color=brightgreen)

Chat with the community on Gitter if you need help:  
  <a href="https://gitter.im/iolevel/peachpie"><img src="https://badges.gitter.im/iolevel/peachpie.svg"></a>

## How to add WordPress into your ASP.NET Core app

Use WordPress as [ASP.NET Core Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-2.2).

1. Add a package reference to `Peachpied.WordPress.AspNetCore` (Pre-Release)
2. Add WordPress as middleware within your `Configure` startup method:

```C#
public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // ...
        app.UseWordPress();
        // ...
    }
}
```

## Configuration

### appsettings.json

WordPress on .NET can be configured using the standard [`appsettings.json`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/), you are no longer editing the `wp-config.php` source file.

```json
{
  "WordPress": {
    "dbhost":        "localhost",
    "dbpassword":    "password",
    "dbuser":        "root",
    "dbname":        "wordpress",
    "dbTablePrefix": "wp_",
    "siteUrl":       "",
    "homeUrl":       "",
    "SALT": {
      "AUTH_KEY":         "r(EoMbKEvlg)",
      "AUTH_SALT":        "q0#AzvJ*[4~B",
      "LOGGED_IN_KEY":    "!AAienFSridC",
      "LOGGED_IN_SALT":   "C=(4(8WPMeRu",
      "NONCE_KEY":        "Z[e37@=y)m.C",
      "NONCE_SALT":       ";v7Wv/BV)Pz{",
      "SECURE_AUTH_KEY":  "pc}_Pv52,m=j",
      "SECURE_AUTH_SALT": "#n]+o^w/%-~M"
    },
    "constants": {
    }
  }
}
```

To generate your unique set of `SALT`, feel free to use Daniel Llewellyn's tool on https://wpdotnet-salts.azurewebsites.net/.

Note: WordPress expects the MySql database to be running, with the database `dbname` (`wordpress` by default) already created. Any of the configuration values can be omitted to use the default value.

### ConfigureServices method

The service can be configured using `AddWordPress` configuration method. First argument is a callback providing options to be modified.

```C#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWordPress(options =>
        {
            options.DbHost = "localhost";
            options.DbName = "wordpress";
            // ...
        });
    }
}
```

The options object is already initialized with default values and with `appsettings.json` configuration.

## Running this repo from the sources:

1. Start mysql, at `localhost:3306`, user `root`, password `password`, created database `wordpress`
2.
    - **cmd:** `dotnet run -p app`
    - **vs:** open solution and start `app` project
    - **vscode**: open folder and hit `F5`


## WordPress on .NET SDK

The solution provides all of WordPress as a .NET standard assembly. Consisting of following projects:

- **app** is a demo project that runs WordPress as a part of ASP.NET Core application.
- **PeachPied.WordPress** contains the sources of WordPress that are compiled into a single .NET standard assembly (`PeachPied.WordPress.dll`). Together with its content files it is packed into a NuGet package `PeachPied.WordPress`. The project contains additional files:
  - `wp-dummy.php` declaring dummy classes required but not used by most plugins to be compiled successfully.
- **PeachPied.WordPress.DotNetBridge** is a "must-use" plugin exposing the WordPress API to .NET.
- **PeachPied.WordPress.Standard** defines an abstraction layer providing predefined PHP constants, and .NET interfaces that get implemented and called by the **DotNetBridge** plugin.
- **PeachPied.WordPress.AspNetCore** is an ASP.NET Core request handler that configures the ASP.NET pipeline to pass requests to compiled `WordPress` scripts. The configuration includes response caching, short URL mapping, various .NET enhancements and the settings of the WordPress database.

This project contains the complete source code of WordPress with the additional "must use" WordPress plugin that exposes the PHP API to .NET. The purpose is for this code to be compiled by PeachPie, resulting in the output running purely on Microsoft .NET Core. 

Therefore, if everything works as it should, you will see the standard unchanged WordPress in the same way as you would in the traditional PHP version. The difference is that the compiled website runs on .NET Core in the background. Also this approach allows to take advantage of ASP.NET Core request handling and makes it possible to extend WordPress with C# (i.e. plugins, themes, etc.).

## Possible Use Cases

- **Improve performance**: compiled code is fast and also optimized by the .NET 'JITter' for your actual system. Additionally, the .NET performance profiler may be used to resolve bottlenecks.
- **Write plugins in C#**: plugin functionality can be implemented in a separate C# project and/or PHP plugins may use .NET libraries. Utilize .NET with all its advantages like sourceless distribution or type safe and compiled code, which is further optimized and checked for the actual platform.
- **Integrate WP into a .NET application**: drive the WordPress life cycle from a C# app, run within the Kestrel Web Server.
- **Sourceless distribution**: after the compilation, most of the source files are not needed. Some files contain meta-data (like plugins and themes) and should be kept in the output.
- **Take advantage** of the .NET runtime: JITted, secure and manageable platform.

## Prerequisites

- .NET Core 3.1 or newer
- MySQL server
- Optionally - Visual Studio 2017 or Visual Studio Code using the official Peachpie for VSCode extension available on the [Marketplace](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode)
