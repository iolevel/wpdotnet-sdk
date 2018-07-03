using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Sdk
{
    /// <summary>Delegate for "the_content" filter.</summary>
    public delegate string the_content_filter(string content);

    /// <summary>Delegate for "the_title" filter.</summary>
    public delegate string the_title_filter(string title, int post_id);

    /// <summary>Delegate for "the_permalink" filter.</summary>
    public delegate string the_permalink_filter(string link, int post_id);

    /// <summary>Delegate to handle shortcode, used in "add_shortcode" function.</summary>
    /// <param name="attrs">Given dictionary of attributes used in shortcode.</param>
    /// <param name="content">Content of the shortcode if any.</param>
    /// <returns>Text to be inserted instead of the shortcode in post.</returns>
    public delegate string shortcode_handler(System.Collections.IDictionary attrs, string content);

    /// <summary>
    /// Provides extension functions to <see cref="IWpApp"/> instances.
    /// </summary>
    public static class WpAppExtension
    {
        /// <summary>
        /// Registers <c>the_content</c> filter.
        /// </summary>
        public static void FilterContent(this IWpApp app, the_content_filter filter) => app.AddFilter("the_content", filter);

        /// <summary>
        /// Registers <c>the_title</c> filter.
        /// </summary>
        public static void FilterTitle(this IWpApp app, the_title_filter filter) => app.AddFilter("the_title", filter);

        /// <summary>
        /// Registers <c>the_permalink</c> filter.
        /// </summary>
        public static void FilterPermalink(this IWpApp app, the_permalink_filter filter) => app.AddFilter("the_permalink", filter);

        /// <summary>
        /// Hooks onto <c>rightnow_end</c> action to provide additional content to the dashboard summary box.
        /// </summary>
        public static void DashboardRightNow(this IWpApp app, Func<string> htmlgetter) => app.AddFilter("rightnow_end", new Action(() => app.Echo(htmlgetter())));
    }
}
