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
        public TimeSpan Interval { get; private set; }

        public Action<Context> Startup { get; private set; }

        static string ScriptPath = "wp-cron.php";

        readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        public WpCronScheduler(Action<Context> startup, TimeSpan interval)
        {
            this.Interval = interval;
            this.Startup = startup;
        }

        void ExecuteAsync()
        {
            try
            {
                using (var ctx = Context.CreateEmpty())
                {
                    Startup(ctx);                   // sets the settings constants
                    ctx.Include(null, ScriptPath);  // include 'wp-cron.php'
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

        public static void StartScheduler(Action<Context> startup, TimeSpan interval)
        {
            var scheduler = new WpCronScheduler(startup, interval);
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