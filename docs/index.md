---
title: Overview
social:
  cards_layout_options:
    title: WpDotNet - WordPress on .NET - Overview
---

# Overview

<div align="center">
  <img src="img/logo.png" width="150">
</div>

[WpDotNet](https://www.wpdotnet.com/) is the unmodified WordPress, running compiled purely on .NET, provided as a NuGet package & ready to be used as a part of an ASP NET Core application. WpDotNet comes with additional components and features, making it easy to be used from C# and a .NET development environment in general.

The project does not require PHP to be installed, and is purely built on top of the .NET platform.

## Features and Use Cases
<div class="grid cards" markdown>

-   :material-clock-fast:{ .lg .middle } __Improved Performance__

    ---

    Compiled code is fast and also optimized by the .NET 'JITter' for your actual system. Additionally, the .NET performance profiler may be used to resolve bottlenecks.

-   :material-vector-combine:{ .lg .middle } __Integrated with .NET__

    ---

     Integrate WordPress into a C# app, and drive its life cycle within the Kestrel Web Server.

-   :material-google-circles-extended:{ .lg .middle } __Extensible by C#__

    ---

    Implement plugins in a separate C# project or have your WP plugins use .NET libraries with type safe and compiled code, optimized and checked for each platform.

-   :material-code-block-braces:{ .lg .middle } __Distributed Without Sources__

    ---

    After the compilation, most of the source files are not needed. Some files contain meta-data (like plugins and themes) and should be kept in the output.

</div>

## Patreon
In general, the WpDotNet project is free and open source, and you can always build it from its sources or use our public NuGet feed, where a stable version is available (check out [how to get started](getting-started.md)). However, you can get access to the most recent versions with all the latest bug fixes and unlock a ton of additional value by becoming [a patron on Patreon](https://www.patreon.com/pchpcompiler). We have two tiers that give you a number of benefits on top of what the open source community gets:

=== "Poweruser"

    * Dedicated, private Discord channel
    * Blogs & Video Tutorials
    * Issue resolution
    * Nightly builds
    * Release builds
    * Access to private NuGet feeds
    * Shout-outs at the end of blogs & videos

=== "Superfan"

    * Everything in the Poweruser tier, plus:
    * Priority issue resolution
    * Private continuous testing
    * Listed as sponsor on our homepage
    * On demand video lessons & tutorials
    * Dedicated, private Discord channel only for Superfans

## Dashboard

Once you deploy WpDotNet, you'll get an informational panel on the Dashboard Home page, within the *At a Glance* widget.

![WpDotNet At Glance](img/wp-dashboard-glance.png)

The panel provides information about the current .NET runtime version, consumed memory, or total CPU time spent in the whole application. Note that the values are reset if the process is restarted.

## Differences

The main differences between regular WordPress running on PHP and WpDotNet running on .NET are:

- The .NET application and all its [plugins/themes](plugins-php.md) need to be compiled before running. Plugins and themes cannot be added after building the project.
- The WordPress configuration is not set in `wp-config.php` file anymore. WpDotNet uses ASP.NET Core configuration providers like `appsettings.json`. See [configuration](configuration.md).
- There is literally no `php` intepreter; all the PHP standard functions and extensions are re-implemented in .NET and their behavior may differ, i.e. break some functionality. In such case please let us know.

## Next Steps

- [Get Started](getting-started.md): See a quick tutorial that will get you started with WpDotNet.
- [Tutorial: Build ASP.NET Core app with WordPress](tutorials/aspnetcore-wordpress.md): Step-by-step creating WordPress app in Visual Studio.
- [Add WordPress Plugins/Themes](plugins-php.md): Extend WpDotNet with WordPress/PHP plugins and themes from `.php` sources.
- [Public NuGet Release](https://www.nuget.org/packages/PeachPied.WordPress.AspNetCore/): Free WpDotNet release versions.