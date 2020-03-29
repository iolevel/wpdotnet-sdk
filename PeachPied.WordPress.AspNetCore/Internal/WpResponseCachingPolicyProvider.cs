using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    // internalized in ASP.NET Core 3.0

    // /// <summary>
    // /// <see cref="IResponseCachingPolicyProvider"/> implementation for WordPress requests.
    // /// </summary>
    // internal sealed class WpResponseCachingPolicyProvider : ResponseCachingPolicyProvider, IWpPlugin
    // {
    //     /// <summary>
    //     /// Time of the last content update.
    //     /// </summary>
    //     DateTime LastPostUpdate = DateTime.UtcNow;

    //     struct Rules
    //     {
    //         public static bool CookieDisallowsCaching(KeyValuePair<string,string> cookie)
    //         {
    //             return
    //                 cookie.Key.StartsWith("wordpress_logged_in", StringComparison.Ordinal) ||   // user is logged in
    //                 cookie.Key.StartsWith("comment_author_", StringComparison.Ordinal);         // user commented something and his comment might be visible only to him
    //         }
    //     }

    //     public override bool AttemptResponseCaching(ResponseCachingContext context)
    //     {
    //         var req = context.HttpContext.Request;

    //         // only GET and HEAD methods are cacheable
    //         if (HttpMethods.IsGet(req.Method) || HttpMethods.IsHead(req.Method))
    //         {
    //             // only if wp user is not logged in
    //             if (!req.Cookies.Any(Rules.CookieDisallowsCaching))
    //             {
    //                 // not wp-admin
    //                 if (!req.Path.Value.Contains("/wp-admin"))
    //                 {
    //                     return true;
    //                 }
    //             }
    //         }

    //         //
    //         return false;
    //     }

    //     public override bool AllowCacheLookup(ResponseCachingContext context)
    //     {
    //         var req = context.HttpContext.Request;

    //         // cache-control: nocache ?
    //         if (HeaderUtilities.ContainsCacheDirective(req.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.NoCacheString))
    //         {
    //             return false;
    //         }

    //         //
    //         return true;
    //     }

    //     public override bool AllowCacheStorage(ResponseCachingContext context)
    //     {
    //         // cache-control: no-store ?
    //         return !HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.NoStoreString);
    //     }

    //     public override bool IsCachedEntryFresh(ResponseCachingContext context)
    //     {
    //         return (context.ResponseTime.Value - context.CachedEntryAge.Value) >= LastPostUpdate;
    //     }

    //     public override bool IsResponseCacheable(ResponseCachingContext context)
    //     {
    //         var responseCacheControlHeader = context.HttpContext.Response.Headers[HeaderNames.CacheControl];

    //         // Check response no-store
    //         if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString))
    //         {
    //             return false;
    //         }

    //         // Check no-cache
    //         if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString))
    //         {
    //             return false;
    //         }

    //         var response = context.HttpContext.Response;

    //         // Do not cache responses with Set-Cookie headers
    //         if (!StringValues.IsNullOrEmpty(response.Headers[HeaderNames.SetCookie]))
    //         {
    //             return false;
    //         }

    //         // Do not cache responses varying by *
    //         var varyHeader = response.Headers[HeaderNames.Vary];
    //         if (varyHeader.Count == 1 && string.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase))
    //         {
    //             return false;
    //         }

    //         // Check private
    //         if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString))
    //         {
    //             return false;
    //         }

    //         // Check response code
    //         if (response.StatusCode != StatusCodes.Status200OK)
    //         {
    //             return false;
    //         }

    //         //
    //         context.HttpContext.Response.Headers[HeaderNames.CacheControl] = StringValues.Concat(CacheControlHeaderValue.SharedMaxAgeString + "=" + 60*60,  responseCacheControlHeader);

    //         //
    //         return true;
    //     }

    //     void IWpPlugin.Configure(WpApp app)
    //     {
    //         Action updated = () =>
    //         {
    //             // existing cache records get invalidated
    //             LastPostUpdate = DateTime.UtcNow;
    //         };

    //         app.AddFilter("save_post", updated);
    //         app.AddFilter("wp_insert_comment", updated);
    //         // edit_comment
    //         // trashed_comment(comment id, comment)
    //         // spammed_comment
    //     }
    // }
}
