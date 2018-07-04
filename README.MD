<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/2/20/WordPress_logo.svg/2000px-WordPress_logo.svg.png" width="400"/>

## WordPress on .NET SDK

The solution compiles all of WordPress into a .NET assembly and additionally provides C# wrappers for utilization of compiled sources. The project consists of several parts:

- `wordpress` contains sources of WordPress that are compiled into a single .NET Core assembly (`wordpress.dll`). Together with its content files it is packed into a NuGet package `PeachPied.WordPress`. The project additionally contains the "must-use" plugin `peachpie-api.php` which exposes the WordPress API to .NET.
- `PeachPied.WordPress.Sdk` defines abstraction layer providing .NET interfaces over PHP WordPress instance. The interface is implemented and provided by `peachpie-api.php`.
- `PeachPied.WordPress.AspNetCore` is an ASP.NET Core request handler that configures the ASP.NET pipeline to pass requests to compiled `WordPress` scripts. The configuration includes response caching, short URL mapping, various .NET enhancements and the settings of WordPress database.
- `app` project is the executable demo ASP.NET Core web server making use of compiled `WordPress`.

## What does this do?

This project contains the complete source code of WordPress with additional "must use" WordPress plugin that exposes PHP API to .NET. The purpose is for this code to be compiled by Peachpie, resulting in the output running purely on Microsoft .NET Core. 

Therefore, if everything works as it should, you will see the standard unchanged WordPress in the same way as you would in the traditional PHP version. The difference is that the compiled website runs on .NET Core in the background. Also this approach allows to take advantage of ASP.NET Core request handling and makes possible to extend WordPress with C# (plugins, themes).

## How does this work?

The PHP sources in `wordpress` directory are compiled to .NET Core by PeachPie compiler, which is seamlessly downloaded by *dotnet* itself.

The sample instantiates Kestrel - the opensource web server - and the ASP.NET Core pipeline. The pipeline handles requests to PHP files using the PeachPie `RequestDelegate` by calling the corresponding compiled scripts in `wordpress.dll`.

Note that the original PHP sources (\*.php files) in `wordpress` are not needed to run the compiled application.

## Possible Use Cases

- **Improve performance**: compiled code is fast and also optimized by the .NET 'JITter' for your actual system. Additionally, the .NET performance profiler may be used to resolve bottlenecks.
- **Write plugins in C#**: plugin functionality can be implemented in a separate C# project and/or PHP plugins may use .NET libraries. Utilize .NET with all its advantages like sourceless distribution or type safe and compiled code, which is further optimized and checked for the actual platform.
- **Integrate WP into a .NET application**: drive the WordPress lifecycle from a C# app, run within the Kestrel Web Server.
- **Sourceless distribution**: after the compilation, most of the source files are not needed. Some files contain meta-data (like plugins and themes) and should be kept in output.
- **Take advantage** of the .NET runtime: JITted, secure and manageable platform.

## Prerequisites

- .NET Core 2.1 or newer
- MySQL server
- Optionally - Visual Studio 2017 or Visual Studio Code using the official Peachpie for VSCode extension available on the [Marketplace](https://marketplace.visualstudio.com/items?itemName=iolevel.peachpie-vscode)

## How to run demo `app`

0. start mysql, at `localhost:3306`, user `root`, password `password`
1. `dotnet run -p app`
