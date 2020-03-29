using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Container of <see cref="IWpPlugin"/> descriptors and singleton instances provider.
    /// </summary>
    public sealed class WpPluginContainer
    {
        class SingletonDescriptor
        {
            public object Instance { get; set; }
            public Type Type { get; set; }
            public object[] Parameters { get; set; }

            public object GetOrCreateInstance(IServiceProvider provider)
            {
                return Instance ??= ActivatorUtilities.CreateInstance(provider, Type, Parameters);
            }
        }

        readonly List<SingletonDescriptor> _descriptors = new List<SingletonDescriptor>();

        /// <summary>
        /// Initialises empty container.
        /// </summary>
        public WpPluginContainer()
        {
        }

        /// <summary>
        /// Initialises container with data from another container.
        /// </summary>
        public WpPluginContainer(WpPluginContainer other)
        {
            if (other != null)
            {
                _descriptors.AddRange(other._descriptors);
            }
        }

        /// <summary>
        /// Gets instances of plugins.
        /// </summary>
        public IEnumerable<IWpPlugin> GetPlugins(IServiceProvider provider)
        {
            return _descriptors
                .Select(x => x.GetOrCreateInstance(provider))
                .OfType<IWpPlugin>();
        }

        /// <summary>
        /// Add plugin descriptor to the container.
        /// </summary>
        public WpPluginContainer Add<T>(params object[] parameters) where T : IWpPlugin
        {
            _descriptors.Add(new SingletonDescriptor
            {
                Type = typeof(T),
                Parameters = parameters ?? Array.Empty<object>(),
            });

            return this;
        }

        /// <summary>
        /// Add plugin descriptor to the container.
        /// </summary>
        public WpPluginContainer Add(IWpPlugin instance)
        {
            _descriptors.Add(new SingletonDescriptor
            {
                Instance = instance ?? throw new ArgumentNullException(nameof(instance)),
            });

            return this;
        }
    }
}
