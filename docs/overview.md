# Overview

[WpDotNet](https://wpdotnet.peachpie.io/) is the latest unmodified WordPress, compiled purely on .NET, available as a NuGet package ready to be used as a part of an ASP NET Core application. As a part of WpDotNet, there are additional components and features making it easy to be used from C# and .NET development environment in general.

The project does not require PHP to be installed, and is purely built on top of .NET platform.

## Requirements

- .NET Core SDK 3.0 or newer. ([dotnet.microsoft.com](https://dotnet.microsoft.com/download) or [visualstudio.microsoft.com](https://visualstudio.microsoft.com/vs/))
- MySQL Server ([dev.mysql.com](https://dev.mysql.com/downloads/mysql/) or [docker](https://hub.docker.com/_/mysql))

Make sure you know valid credentials to your MySQL server and you have created a database in it. The following quick start expects a database named `"wordpress"`. Database charset `"UTF-8"` is recommended.

## Quick Start

> A demo wordpress application is available at [github.com/iolevel/peachpie-wordpress](https://github.com/iolevel/peachpie-wordpress).

Open or create an ASP NET Core application, version 3.0 or newer.

Add a package reference to [`"Peachpied.WordPress.AspNetCore"`](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/) (note it is a pre-release package).

Add WordPress middleware within your request pipeline, in `Configure` startup method:

```C#
public partial class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // ...
        app.UseWordPress();
        // ...
    }
}
```

Add WordPress option service within `ConfigureServices` startup method and setup your database connection and other options:

```C#
public partial class Startup
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

> Note: A recommended approach is to place the configuration within `appsettings.json` configuration file. See [configuration](../configuration) for more details.

## Remarks

- permalinks are implicitly enabled through URL rewriting feature.
- wordpress debugging is implicitlt enabled when running in *Development* environment.
- When running on Azure with MySql in App enbaled, database connection is automatically configured.
- Response caching and response compression is enabled by default.
- Most of original `.php` files are not present on file system and cannot be edited.

## Related links

- https://wpdotnet.peachpie.io/
- https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/ [![NuGet](https://img.shields.io/nuget/v/PeachPied.WordPress.AspNetCore.svg)](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/)
