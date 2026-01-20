/*
 * Demo application with WordPress.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // Autoinstall Wordpress Database on first init with Username: admin, Password: admin
        // Íf WPInstall is set to false or UseMySQL is true this is getting ignored open /wp-admin/install.php and install manually
        public class InitWordpressDatabase : BackgroundService
        {
            private readonly IServiceProvider _services;
            private readonly IHostApplicationLifetime _lifetime;
            private readonly IConfiguration _configuration;

            public InitWordpressDatabase(IServiceProvider services, IHostApplicationLifetime lifetime, IConfiguration configuration)
            {
                _services = services;
                _lifetime = lifetime;
                _configuration=configuration;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var multiForm = new MultipartFormDataContent();
                multiForm.Add(new StringContent(_configuration.GetValue<string>("WordPress:WPInstallWeblogTitle")), "weblog_title");
                multiForm.Add(new StringContent(_configuration.GetValue<string>("WordPress:WPInstallUserName")), "user_name");
                multiForm.Add(new StringContent(_configuration.GetValue<string>("WordPress:WPInstallAdminPassword")), "admin_password");
                multiForm.Add(new StringContent(_configuration.GetValue<string>("WordPress:WPInstallAdminPassword")), "admin_password2");
                multiForm.Add(new StringContent(_configuration.GetValue<string>("WordPress:WPInstallAdminEmail")), "admin_email");
                multiForm.Add(new StringContent("Install+WordPress"), "Submit");

                int port = 80;
                Uri uri = new Uri("http://127.0.0.1/");
                string url = uri.ToString();

                foreach (var address in _services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses)
                {
                    uri = new Uri(address.Replace("*", "127.0.0.1"));
                    port = uri.Port;
                }

                if (port != 80)
                {
                    url = $"{uri.ToString()}";
                }
                var response = await new HttpClient().PostAsync(url + "wp-admin/install.php?step=2", multiForm);
                Console.WriteLine("--------------------------------------------------------------------");
                Console.WriteLine("\tWordpress Database created");
                Console.WriteLine("");
                Console.WriteLine("\tWordpress Weblog Title:" + _configuration.GetValue<string>("WordPress:WPInstallWeblogTitle"));
                Console.WriteLine("\tWordpress Admin User Name:" + _configuration.GetValue<string>("WordPress:WPInstallUserName"));
                Console.WriteLine("\tWordpress Admin Password:" + _configuration.GetValue<string>("WordPress:WPInstallAdminPassword"));
                Console.WriteLine("\tWordpress Admin eMail:" + _configuration.GetValue<string>("WordPress:WPInstallAdminEmail"));
                Console.WriteLine("");
                Console.WriteLine("\tPlease open: " + url);
                Console.WriteLine("--------------------------------------------------------------------");
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            services.AddWordPress(options =>
            {
                //
            });

            // Check if we want to use SQLite, if so check if database folders exists and database is not empty, trigger install if set in config 
            bool needDatabaseInit = false;
            if (!Configuration.GetValue<bool>("WordPress:UseMySQL") && Configuration.GetValue<bool>("WordPress:WPInstall"))
            {
                if (!Directory.Exists(Configuration.GetValue<string>("WordPress:SQLiteFolder")))
                {
                    Directory.CreateDirectory(Configuration.GetValue<string>("WordPress:SQLiteFolder"));
                }

                if (File.Exists(Configuration.GetValue<string>("WordPress:SQLiteFolder") + Configuration.GetValue<string>("WordPress:SQLiteFileName")))
                {
                    FileInfo fi = new FileInfo(Configuration.GetValue<string>("WordPress:SQLiteFolder") + Configuration.GetValue<string>("WordPress:SQLiteFileName"));
                    if (fi.Length < 100000)
                    {
                        needDatabaseInit = true;
                    }
                } else
                {
                    needDatabaseInit = true;
                }

                if (needDatabaseInit)
                {
                    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    services.AddHostedService<InitWordpressDatabase>();
                }
            }
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
