using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    static class ServiceProviderExtensions
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        public static T GetService<T>(this IServiceProvider services) where T : class => services?.GetService(typeof(T)) as T;

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        public static bool TryGetService<T>(this IServiceProvider services, out T service) where T : class
        {
            service = services?.GetService(typeof(T)) as T;
            return service != null;
        }
    }
}
