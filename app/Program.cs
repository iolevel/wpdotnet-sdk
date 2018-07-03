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
            // make sure cwd is not app\ but its parent:
            if (Path.GetFileName(Directory.GetCurrentDirectory()) == "app")
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            }

            //
            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://*:5004/")
                .Build();

            host.Run();
        }
    }

    class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IConfiguration configuration)
        {
            // settings:
            var wpconfig = new WordPressConfig();
            configuration
                .GetSection("WordPress")
                .Bind(wpconfig);

            //
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWordPress(wpconfig);

            app.UseDefaultFiles();
        }
    }
}
