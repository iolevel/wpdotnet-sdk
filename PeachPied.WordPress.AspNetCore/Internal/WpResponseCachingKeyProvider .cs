using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    // internalized in ASP.NET Core 3.0
    
    // /// <summary>
    // /// <see cref="IResponseCachingKeyProvider"/> implementation for WordPress requests.
    // /// </summary>
    // internal sealed class WpResponseCachingKeyProvider : ResponseCachingKeyProvider
    // {
    //     // Use the record separator for delimiting components of the cache key to avoid possible collisions
    //     const char KeyDelimiter = '\x1e';

    //     // Use the unit separator for delimiting subcomponents of the cache key to avoid possible collisions
    //     const char KeySubDelimiter = '\x1f';

    //     public WpResponseCachingKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options)
    //      : base(poolProvider, options)
    //     {
    //     }

    //     public string CreateBaseKey(ResponseCachingContext context)
    //     {
    //         var request = context.HttpContext.Request;

    //         return new StringBuilder(128)
    //             .Append(request.Method)
    //             .Append(KeyDelimiter)
    //             .Append(request.Scheme)
    //             .Append(KeyDelimiter)
    //             .AppendUpperInvariant(request.Host.Value)
    //             .Append(request.PathBase.Value)
    //             .Append(request.Path.Value)
    //             .Append(request.QueryString.Value)
    //             .ToString();
    //     }

    //     public IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context)
    //     {
    //         return new string[] { CreateStorageVaryByKey(context) };
    //     }

    //     public string CreateStorageVaryByKey(ResponseCachingContext context)
    //     {
    //         if (context == null)
    //         {
    //             throw new ArgumentNullException(nameof(context));
    //         }

    //         var varyByRules = context.CachedVaryByRules;
    //         if (varyByRules == null)
    //         {
    //             return string.Empty;
    //         }

    //         if ((StringValues.IsNullOrEmpty(varyByRules.Headers) && StringValues.IsNullOrEmpty(varyByRules.QueryKeys)))
    //         {
    //             return varyByRules.VaryByKeyPrefix;
    //         }

    //         var request = context.HttpContext.Request;
    //         var builder = new StringBuilder();

    //         // Prepend with the Guid of the CachedVaryByRules
    //         builder.Append(varyByRules.VaryByKeyPrefix);

    //         // Vary by headers
    //         if (varyByRules?.Headers.Count > 0)
    //         {
    //             // Append a group separator for the header segment of the cache key
    //             builder
    //                 .Append(KeyDelimiter)
    //                 .Append('H');

    //             for (var i = 0; i < varyByRules.Headers.Count; i++)
    //             {
    //                 var header = varyByRules.Headers[i];
    //                 var headerValues = context.HttpContext.Request.Headers[header];
    //                 builder
    //                     .Append(KeyDelimiter)
    //                     .Append(header)
    //                     .Append('=');

    //                 var headerValuesArray = headerValues.ToArray();
    //                 Array.Sort(headerValuesArray, StringComparer.Ordinal);

    //                 for (var j = 0; j < headerValuesArray.Length; j++)
    //                 {
    //                     builder.Append(headerValuesArray[j]);
    //                 }
    //             }
    //         }

    //         // Vary by query keys
    //         if (varyByRules?.QueryKeys.Count > 0)
    //         {
    //             // Append a group separator for the query key segment of the cache key
    //             builder
    //                 .Append(KeyDelimiter)
    //                 .Append('Q');

    //             if (varyByRules.QueryKeys.Count == 1 && string.Equals(varyByRules.QueryKeys[0], "*", StringComparison.Ordinal))
    //             {
    //                 // Vary by all available query keys
    //                 var queryArray = context.HttpContext.Request.Query.ToArray();
    //                 // Query keys are aggregated case-insensitively whereas the query values are compared ordinally.
    //                 Array.Sort(queryArray, QueryKeyComparer.OrdinalIgnoreCase);

    //                 for (var i = 0; i < queryArray.Length; i++)
    //                 {
    //                     builder
    //                         .Append(KeyDelimiter)
    //                         .AppendUpperInvariant(queryArray[i].Key)
    //                         .Append('=');

    //                     var queryValueArray = queryArray[i].Value.ToArray();
    //                     Array.Sort(queryValueArray, StringComparer.Ordinal);

    //                     for (var j = 0; j < queryValueArray.Length; j++)
    //                     {
    //                         if (j > 0)
    //                         {
    //                             builder.Append(KeySubDelimiter);
    //                         }

    //                         builder.Append(queryValueArray[j]);
    //                     }
    //                 }
    //             }
    //             else
    //             {
    //                 for (var i = 0; i < varyByRules.QueryKeys.Count; i++)
    //                 {
    //                     var queryKey = varyByRules.QueryKeys[i];
    //                     var queryKeyValues = context.HttpContext.Request.Query[queryKey];
    //                     builder
    //                         .Append(KeyDelimiter)
    //                         .Append(queryKey)
    //                         .Append('=');

    //                     var queryValueArray = queryKeyValues.ToArray();
    //                     Array.Sort(queryValueArray, StringComparer.Ordinal);

    //                     for (var j = 0; j < queryValueArray.Length; j++)
    //                     {
    //                         if (j > 0)
    //                         {
    //                             builder.Append(KeySubDelimiter);
    //                         }

    //                         builder.Append(queryValueArray[j]);
    //                     }
    //                 }
    //             }
    //         }

    //         //

    //         return builder.ToString();
    //     }

    //     private class QueryKeyComparer : IComparer<KeyValuePair<string, StringValues>>
    //     {
    //         private StringComparer _stringComparer;

    //         public static QueryKeyComparer OrdinalIgnoreCase { get; } = new QueryKeyComparer(StringComparer.OrdinalIgnoreCase);

    //         public QueryKeyComparer(StringComparer stringComparer)
    //         {
    //             _stringComparer = stringComparer;
    //         }

    //         public int Compare(KeyValuePair<string, StringValues> x, KeyValuePair<string, StringValues> y) => _stringComparer.Compare(x.Key, y.Key);
    //     }

    // }
}
