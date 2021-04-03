using System;
using Pchp.Core;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Represents a WordPress website.
    /// </summary>
    [PhpType]   // The class is visible to the PHP code that references this assembly.
    public abstract class WpApp
    {
        /// <summary>
        /// Do not modify.
        /// Well-known form recognized by the compiler.
        /// </summary>
        protected readonly Context _ctx;

        /// <summary>
        /// Minimal constructor that initializes runtime context.
        /// The .ctor is called implicitly by derived PHP class.
        /// </summary>
        protected WpApp(Context ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <summary>
        /// Runtime context of the application.
        /// Representing the current web request.
        /// </summary>
        public Context Context => _ctx;

        /// <summary>
        /// Calls <c>add_filter</c> function, see https://developer.wordpress.org/reference/functions/add_filter/.
        /// </summary>
        public abstract void AddFilter(string tag, Delegate @delegate, long priority = 10, long accepted_args = 1);

        /// <summary>
        /// Adds shortcode.
        /// </summary>
        /// <param name="tag">Shortcode tag name.</param>
        /// <param name="delegate"><see cref="shortcode_handler"/> to process shortcode in posts.</param>
        public abstract void AddShortcode(string tag, Delegate @delegate);
    }
}
