using System;

namespace PeachPied.WordPress.Sdk
{
    /// <summary>
    /// Represents a WordPress website.
    /// </summary>
    public interface IWpApp
    {
        /// <summary>
        /// Gets WordPress version string.
        /// </summary>
        string GetVersion();

        /// <summary>
        /// Calls <c>add_filter</c> function, see https://developer.wordpress.org/reference/functions/add_filter/.
        /// </summary>
        void AddFilter(string tag, Delegate @delegate);

        /// <summary>
        /// Adds shortcode.
        /// </summary>
        /// <param name="tag">Shortcode tag name.</param>
        /// <param name="delegate"><see cref="shortcode_handler"/> to process shortcode in posts.</param>
        void AddShortcode(string tag, Delegate @delegate);

        /// <summary>
        /// Writes text to the output.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Echo(string text);
    }
}
