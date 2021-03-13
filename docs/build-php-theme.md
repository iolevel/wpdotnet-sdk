# Build a PHP theme

Single theme for WordPress is defined as a class library project which is then referenced by the application. Application can have references to one or more themes.

## Theme source files

Sources of the theme complies with the regular WordPress codex. See [Theme Handbook](https://developer.wordpress.org/themes/) for more details.

Put the theme source files in an empty folder. Name or location of the folder are both not relevant.

## Theme project file

In order to compile the theme, create a project file in the theme folder.

```shell
┬ MyWordPressTheme
└┬ MyWordPressTheme.msbuildproj  <---
 ├ readme.txt
 └ ...
```

By default, the name of the project file represents the theme's *id*. *Id*, or *slug id* is the identifier of the theme within WordPress. Also it denotates the folder name in which the theme files will be placed within `wp-content` directory.

Project file is an XML file with the following content:

> *MyWordPressTheme.msbuildproj:*
```xml
<Project Sdk="PeachPied.WordPress.Build.Plugin/5.7.0-preview6">
  <PropertyGroup>
    <WpContentTarget>themes</WpContentTarget>
  </PropertyGroup>
</Project>
```

Note the project file specifies a version after the slash, i.e. `"/5.7.0-preview6"`. This corresponds to the version of *PeachPied.WordPress.** packages which should be identical across all your application.

## Build the theme

In order to build the theme, run `dotnet build` command:

```shell
dotnet build
```

The process will build the theme project. Eventual warnings will be outputed. Any error in the code will cause the build to fail.

> If the process is run for the first time, it will first download the project Sdk and dependency packages (it means it requires an Internet connection).

### Add theme to the application

Assuming you have ASP.NET Core Application (called *app*) with WordPress (see [quick start](../overview/#quick-start)). Adding themes to the application is equivalent to adding project references or package references.

Either add project reference to the theme in Visual Studio IDE, or on command line, or edit the application's project file:

> *app.csproj:*
```xml
<ItemGroup>
  <ProjectReference Include="../MyWordPressTheme/MyWordPressTheme.msbuildproj" />
</ItemGroup>
```

> Note, the theme is referenced by the application and it still needs to be activated in WordPress dashboard.

## Package the theme

Theme can be packed into a standard NuGet package. Run `dotnet pack` command to create NuGet package:

```shell
dotnet pack
```

Package (*.nupkg*) can be pushed into a NuGet feed. It is recommended to push packages to private feeds, or at least pack packages with a unique prefixed package identifier (i.e. *MyName.MyWpThemes.MyWordPressTheme*).

### Add package reference to the application

Once the theme is packed and available on a NuGet feed, it can be referenced as a package reference. Either add package reference to the theme in Visual Studio IDE, or on command line, or edit the application's project file:

```xml
<ItemGroup>
  <PackageReference Include="MyWordPressTheme" Version="1.0.0" />
</ItemGroup>
```

## Debugging the theme

> not yet documented