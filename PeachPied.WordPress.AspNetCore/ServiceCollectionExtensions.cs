using Microsoft.Extensions.DependencyInjection;
using PeachPied.WordPress.AspNetCore;
using PeachPied.WordPress.AspNetCore.Internal;
using PeachPied.WordPress.Standard;
using System;

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
            services.AddPhp(options =>
            {
            });

            services.Configure(configure);

            services.AddSingleton<IWpPluginLogger, PeachPied.WordPress.AspNetCore.Internal.Logger>();

            //
            return services;
        }

        ///// <summary>
        ///// Registers all shared views as WordPress shortcodes correspondingly.
        ///// </summary>
        ///// <remarks>Same as configuring WordPress with <see cref="PeachPied.WordPress.AspNetCore.WpAppExtension.RegisterPartialViewAsShortcode"/> for each shared view.</remarks>
        //public static IServiceCollection AddWordPressShortcodesFromViews(this IServiceCollection services)
        //{
        //    if (services == null)
        //    {
        //        throw new ArgumentNullException(nameof(services));
        //    }

        //    services.Add(new ServiceDescriptor(
        //        typeof(IWpPluginProvider),
        //        typeof(ViewsAsShortcodesPlugin),
        //        ServiceLifetime.Transient
        //        ));

        //    //
        //    return services;
        //}
    }
}
