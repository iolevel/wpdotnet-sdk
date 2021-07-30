/*
 * Demo application with WordPress.
 */

using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace peachserver
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://*:80/")
                .Build();

            host.Run();
        }
    }

    class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            services.AddWordPress(options =>
            {
                //

                // multisite scenario
                
                // 1
                /*options.AllowMultiSite = true;*/

                // 2
                /*
                options.MultiSite = true;
                options.SubdomainInstall = false;
                options.DomainCurrentSite = "localhost";
                options.CurrentSitePath = "/";
                options.SiteIDCurrentSite = 1;
                options.BlogIDCurrentSite = 1;
                */
            });
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env, IConfiguration configuration)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // add wordpress into the pipeline
            // using default configuration from appsettings.json (IConfiguration), section WordPress
            // using empty set of .NET plugins
            app.UseWordPress();

            app.UseDefaultFiles();
        }
    }
}
