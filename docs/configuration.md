# Configuration

The WpDotNet application configuration is performed using 
- ASP.NET Core configuration providers and settings files, such as `appsettings.json`
- Registering options in `ConfigureServices` startup method.

Read more about the [configuration on docs.microsoft.com](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).

## Default configuration

By default, an ASP.NET Core application reads configurations from setting files (using *JSON configuration provider*). The settings are read hierarchically in the following order:

1. *appsettings.json*.
2. *appsettings.`{Environment}`.json*. For example, `appsettings.Production.json` or `appsettings.Development.json`.

The `{Environment}` value is controlled using the process's environment variable `ASPNETCORE_ENVIRONMENT`. If not specified, the default value is `"Production"`. Read more at [environments on docs.microsoft.com](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments).

**Sample appsettings.json**

```json
{
  "WordPress": {
    "dbhost": "localhost",
    "dbpassword": "password",
    "dbuser": "root",
    "dbname": "wordpress",
  }
}
```

The configuration is placed in the section `"WordPress": { ... }`. This allows to set the following options:

- *DbHost*: MySql database server address, including an optional port number. Default is `"localhost:3306"`.
- *DbUser*: database user name to connect with.
- *DbPassword*: database password.
- *DbName*: database name. It is expected the database is created already, using charset `utf8`.
- *DbTablePrefix*: optional prefix to database tables.
- *SiteUrl*: Optional URL where WordPress core files reside. It should include the http(s):// part, without trailing slash `"/"` at the end.
- *HomeUrl*: Optional value which represents public URL of the WordPress blog. It should include the http(s):// part, without trailing slash `"/"` at the end. Adding this can reduce the number of database calls when loading the site.
- *Constants*: Object with name-values allowing to define additional runtime constants.
- *LegacyPluginAssemblies*: an array of assembly name values, each assembly represents a compiled PHP plugin, theme, or `wp-content` in general. This option will become deprecated as it won't be needed anymore, for now it is needed to be specified.

> Note, since the setting files are hierarchical, the best practice is to set the common options in *appsettings.json*, and an environment-specific options in *appsettings.`{Environment}`.json*. This allows to maintain multiple environments, i.e. a different database connection for a different host providers, staging, and development environments, at once.

## ConfigureServices

Register options in `ConfigureServices` startup method to programically control the application settings.

**Sample ConfigureServices method**

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


- Azure's MySQL In App
- Cache
- HomeUrl
- Multisite