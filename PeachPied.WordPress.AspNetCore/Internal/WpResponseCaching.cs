using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal class WpResponseCacheMiddleware
    {
        readonly RequestDelegate _next;
        readonly WpResponseCachePolicy _policy;
        readonly ILogger _logger;
        readonly IMemoryCache _cache;

        class CacheKey : IEquatable<CacheKey>
        {
            public string Method, Host, PathBase, Path, QueryString;

            public CacheKey(HttpContext context)
            {
                Method = context.Request.Method;
                Host = context.Request.Host.Value ?? string.Empty;
                PathBase = context.Request.PathBase.Value ?? string.Empty;
                Path = context.Request.Path.Value ?? string.Empty;
                QueryString = context.Request.QueryString.Value ?? string.Empty;
            }

            public override int GetHashCode() =>
                Method.GetHashCode() ^
                Host.GetHashCode() ^
                PathBase.GetHashCode() ^
                Path.GetHashCode() ^
                QueryString.GetHashCode();

            public override bool Equals(object obj) => obj is CacheKey key && Equals(key);

            public bool Equals(CacheKey other)
            {
                return
                    other != null &&
                    other.Method == this.Method &&
                    other.Host == this.Host &&
                    other.PathBase == this.PathBase &&
                    other.Path == this.Path &&
                    other.QueryString == this.QueryString;
            }
        }

        class CachedPage
        {
            public byte[] Content { get; private set; }
            public List<KeyValuePair<string, StringValues>> Headers { get; private set; }
            public DateTime TimeStamp { get; private set; }

            public CachedPage(byte[] content)
            {
                Content = content;
                Headers = new List<KeyValuePair<string, StringValues>>();
                TimeStamp = DateTime.UtcNow;
            }

            public CachedPage(HttpContext context, byte[] content)
                : this(content)
            {
                this.Headers.AddRange(context.Response.Headers);
            }
        }

        async Task<CachedPage> CaptureResponse(HttpContext context)
        {
            var responseStream = context.Response.Body;

            using (var buffer = new MemoryStream())
            {
                try
                {
                    context.Response.Body = buffer;

                    await _next.Invoke(context);
                }
                finally
                {
                    context.Response.Body = responseStream;
                }

                if (buffer.Length == 0) return null;

                byte[] bytes;

                // gzip response
                if (ShouldCompressResponse(context))
                {
                    context.Response.Headers.Append(HeaderNames.ContentEncoding, "gzip");
                    context.Response.Headers.Remove(HeaderNames.ContentMD5); // Reset the MD5 because the content changed.
                    context.Response.Headers.Remove(HeaderNames.ContentLength);

                    using (var gzipStream = new MemoryStream((int)(buffer.Length / 2)))
                    {
                        using (var stream = new GZipStream(gzipStream, CompressionLevel.Optimal, leaveOpen: true))
                        {
                            buffer.Position = 0;
                            await buffer.CopyToAsync(stream);
                        }

                        bytes = gzipStream.ToArray();
                    }
                }
                else
                {
                    bytes = buffer.ToArray();
                }

                var cached = IsResponseCacheable(context) ? new CachedPage(context, bytes) : null;

                //
                await responseStream.WriteAsync(bytes, 0, bytes.Length);
                return cached;
            }
        }

        static bool ShouldCompressResponse(HttpContext context)
        {
            if (context.Response.Headers.ContainsKey(HeaderNames.ContentRange))
            {
                return false;
            }

            if (context.Response.Headers.ContainsKey(HeaderNames.ContentEncoding))
            {
                return false;
            }

            var mimeType = context.Response.ContentType;

            if (string.IsNullOrEmpty(mimeType))
            {
                return false;
            }

            var separator = mimeType.IndexOf(';');
            if (separator >= 0)
            {
                // Remove the content-type optional parameters
                mimeType = mimeType.Substring(0, separator).Trim();
            }

            var shouldCompress = mimeType.StartsWith("text/");

            if (shouldCompress)
            {
                return true;
            }

            return false;
        }

        async Task WriteResponse(HttpContext context, CachedPage page)
        {
            foreach (var header in page.Headers)
            {
                context.Response.Headers.Add(header);
            }

            await context.Response.Body.WriteAsync(page.Content, 0, page.Content.Length);
        }

        public WpResponseCacheMiddleware(RequestDelegate next, IMemoryCache cache, WpResponseCachePolicy policy, ILoggerFactory loggerFactory)
        {
            _next = next;
            _cache = cache;
            _policy = policy;
            _logger = loggerFactory.CreateLogger<WpResponseCacheMiddleware>();
        }

        CacheKey BuildCacheKey(HttpContext context) => new CacheKey(context);

        static bool CookieDisallowsCaching(KeyValuePair<string, string> cookie)
        {
            return
                cookie.Key.StartsWith("wordpress_logged_in", StringComparison.Ordinal) ||   // user is logged in // TODO: sometimes it is an incorrect login
                cookie.Key.StartsWith("comment_author_", StringComparison.Ordinal);         // user commented something and his comment might be visible only to him
        }

        bool IsCacheable(HttpContext context)
        {
            var req = context.Request;

            // only GET and HEAD methods are cacheable
            if (HttpMethods.IsGet(req.Method) || HttpMethods.IsHead(req.Method))
            {
                // only if wp user is not logged in
                if (!req.Cookies.Any(CookieDisallowsCaching))
                {
                    // not wp-admin
                    if (!req.Path.Value.Contains("/wp-admin"))
                    {
                        return AllowCacheStorage(context);
                    }
                }
            }

            //
            return false;
        }

        static bool AllowCacheLookup(HttpContext context)
        {
            var req = context.Request;

            // cache-control: nocache ?
            if (HeaderUtilities.ContainsCacheDirective(req.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }

            // logged user or posting comment:
            if (req.Cookies.Any(CookieDisallowsCaching))
            {
                return false;
            }

            //
            return true;
        }

        static bool AllowCacheStorage(HttpContext context)
        {
            // cache-control: no-store ?
            return !HeaderUtilities.ContainsCacheDirective(context.Request.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.NoStoreString);
        }

        static bool IsResponseCacheable(HttpContext context)
        {
            var responseCacheControlHeader = context.Response.Headers[HeaderNames.CacheControl];

            // Check response no-store
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString))
            {
                return false;
            }

            // Check no-cache
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }

            var response = context.Response;

            // Do not cache responses with Set-Cookie headers
            if (!StringValues.IsNullOrEmpty(response.Headers[HeaderNames.SetCookie]))
            {
                return false;
            }

            // Do not cache responses varying by *
            var varyHeader = response.Headers[HeaderNames.Vary];
            if (varyHeader.Count == 1 && string.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check private
            if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString))
            {
                return false;
            }

            // Check response code
            if (response.StatusCode != StatusCodes.Status200OK)
            {
                return false;
            }

            // //
            // context.Response.Headers[HeaderNames.CacheControl] = StringValues.Concat(CacheControlHeaderValue.SharedMaxAgeString + "=" + 60*60,  responseCacheControlHeader);

            //
            return true;
        }

        public async Task Invoke(HttpContext context)
        {
            if (AllowCacheLookup(context))
            {
                var key = BuildCacheKey(context);

                if (_cache.TryGetValue(key, out CachedPage page) && _policy.LastPostUpdate < page.TimeStamp)
                {
                    _logger.LogInformation("Response served from cache.");
                    await WriteResponse(context, page);
                    return;
                }

                if (IsCacheable(context))
                {
                    page = await CaptureResponse(context);

                    if (page != null) // response is cacheable
                    {
                        _logger.LogInformation("Response cached.");
                        _cache.Set(key, page, TimeSpan.FromMinutes(60.0));
                        // var serverCacheDuration = GetCacheDuration(context, Constants.ServerDuration);

                        // if (serverCacheDuration.HasValue)
                        // {
                        //     var tags = GetCacheTags(context, Constants.Tags);

                        //     _cache.Set(key, page, serverCacheDuration.Value, tags);
                        // }
                    }

                    return;
                }
            }

            // default
            await _next.Invoke(context);
            return;
        }
    }

    internal class WpResponseCachePolicy : IWpPlugin
    {
        public DateTime LastPostUpdate { get; private set; } = DateTime.UtcNow;

        void IWpPlugin.Configure(WpApp app)
        {
            Action updated = () =>
            {
                // existing cache records get invalidated
                LastPostUpdate = DateTime.UtcNow;
            };

            app.AddFilter("save_post", updated); // post updated
            app.AddFilter("wp_insert_comment", updated);    // comment added
            app.AddFilter("activate_plugin", updated); // plugin activated
            app.AddFilter("deactivate_plugin", updated); // plugin deactivated
            app.AddFilter("switch_theme", updated); // theme changed

            // TODO: edit_comment
            // TODO: trashed_comment(comment id, comment)
            // TODO: spammed_comment
        }
    }
}