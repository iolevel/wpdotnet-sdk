using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pchp.Core;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    // simple background scheduler that invokes wp-cron.php in specified interval.
    sealed class WpCronScheduler
    {
        public TimeSpan Interval { get; }

        public Action<Context> Startup { get; }

        public string RootPath { get; }

        readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public WpCronScheduler(Action<Context> startup, TimeSpan interval, string rootpath)
        {
            this.Interval = interval;
            this.Startup = startup;
            this.RootPath = rootpath;
        }

        void ExecuteAsync()
        {
            try
            {
                using (var ctx = Context.CreateEmpty())
                {
                    ctx.RootPath = ctx.WorkingDirectory = RootPath;        // wordpress content files location
                    Startup(ctx);                   // sets the settings constants
                    ctx.Include("", "wp-load.php", once: true); // include 'wp-load.php'
                    ctx.Include("", "wp-cron.php");             // include 'wp-cron.php'
                }
            }
            catch
            {
                // TODO: log the exception
            }
        }

        public void Stop()
        {
            _cancel.Cancel();
        }

        public static void StartScheduler(Action<Context> startup, TimeSpan interval, string rootpath)
        {
            var scheduler = new WpCronScheduler(startup, interval, rootpath);
            scheduler.Start();
        }

        public async void Start()
        {
            while (!_cancel.IsCancellationRequested)
            {
                await Task.Delay(Interval);
                ExecuteAsync();
            }
        }
    }
}