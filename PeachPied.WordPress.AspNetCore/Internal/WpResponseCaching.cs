using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using PeachPied.WordPress.Sdk;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal class WpResponseCacheMiddleware
    {
        readonly RequestDelegate _next;
        readonly WpResponseCachePolicy _policy;
        readonly ILoggerFactory _loggerFactory;
        readonly IMemoryCache _cache;

        public WpResponseCacheMiddleware(RequestDelegate next, IMemoryCache cache, WpResponseCachePolicy policy, ILoggerFactory loggerFactory)
        {
            _next = next;
            _cache = cache;
            _policy = policy;
            _loggerFactory = loggerFactory;
        }

        public Task Invoke(HttpContext httpContext)
        {
            // TODO: cache

            return _next(httpContext);
        }
    }

    internal class WpResponseCachePolicy : IWpPlugin
    {
        DateTime _lastPostUpdate = DateTime.UtcNow;

        void IWpPlugin.Configure(WpApp app)
        {
            Action updated = () =>
            {
                // existing cache records get invalidated
                _lastPostUpdate = DateTime.UtcNow;
            };

            app.AddFilter("save_post", updated);
            app.AddFilter("wp_insert_comment", updated);

            // TODO: edit_comment
            // TODO : trashed_comment(comment id, comment)
            // TODO: spammed_comment
        }
    }
}