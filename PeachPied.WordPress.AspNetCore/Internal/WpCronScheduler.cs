using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    // simple background scheduler that fires requests in specified interval.
    sealed class WpCronScheduler
    {
        TimeSpan Interval { get; set; }

        // TODO: if there are lots of requests, postpone execution of the task

        readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        readonly string _method;
        readonly Uri _uri;

        public WpCronScheduler(string method, Uri uri, TimeSpan interval)
        {
            _method = method ?? HttpMethods.Get;
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));

            this.Interval = interval;
        }

        void ExecuteAsync()
        {
            var request = HttpWebRequest.CreateHttp(_uri);
            request.Method = _method;
            try
            {
                request.GetResponseAsync();
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

        public static void StartScheduler(string method, Uri uri, TimeSpan interval)
        {
            var scheduler = new WpCronScheduler(method, uri, interval);
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