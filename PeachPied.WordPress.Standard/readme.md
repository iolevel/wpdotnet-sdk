The API for integrating .NET plugins and to bridge between C# and WordPress PHP code in general.

### Plugin in C#
- Implement interface `IWpPlugin` and hook your filters to `IWpApp`.
- Provide the plugin instance to `WpLoader` (implemented in `Peachpie.WordPress.AspNetCore` package)
