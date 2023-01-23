#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pchp.Core;
using Peachpie.AspNetCore.Mvc;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.AspNetCore
{
    /// <summary>
    /// Provides extension functions to <see cref="WpApp"/> instances.
    /// </summary>
    [PhpHidden]
    public static class WpAppExtension
    {
        /// <summary>
        /// Register shortcode that renders shared partial view (<c>.cshtml</c> file).
        /// </summary>
        public static void RegisterPartialViewAsShortcode(this WpApp app, string viewName, PhpValue viewModel = default(PhpValue))
        {
            app.AddShortcode(viewName, new shortcode_handler((attrs, _, _) => app.Context.Partial(viewName)));
        }
    }
}
