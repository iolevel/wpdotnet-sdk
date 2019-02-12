using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PeachPied.WordPress.AspNetCore;

namespace peachserver
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://*:5004/")
                .Build();

            host.Run();
        }
    }

    class Startup
    {
        /// <summary>
        /// This is only needed when running within this solution.
        /// </summary>
        /// <returns>
        /// Gets directory where <c>wordpress</c> resources are located.
        /// <c>null</c> to use the current bin directory.
        /// </returns>
        static string DirectoryWithWordPress()
        {
            // make sure cwd is not app\ but its parent:
            if (Path.GetFileName(Directory.GetCurrentDirectory()) == "app")
            {
                return Path.Combine(Path.GetDirectoryName(Directory.GetCurrentDirectory()), "wordpress");
            }

            // request handler will locate "wordpress" in current bin directory
            return null;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IConfiguration configuration)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // add wordpress into the pipeline
            // using default configuration from appsettings.json (IConfiguration), section WordPress
            // using empty set of .NET plugins
            app.UseWordPress(path: DirectoryWithWordPress());

            app.UseDefaultFiles();
        }
    }
}
