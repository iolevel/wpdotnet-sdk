using Microsoft.Extensions.DependencyInjection;
using PeachPied.WordPress.AspNetCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Configuration extension methods.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds optional WordPress services and configuration callback.
        /// </summary>
        public static IServiceCollection AddWordPress(this IServiceCollection services, Action<WordPressConfig> configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // 
            services.AddPeachpie();
            services.Configure(configure);

            //
            return services;
        }
    }
}
