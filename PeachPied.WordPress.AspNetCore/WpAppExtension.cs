﻿#nullable enable

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
        /// <br/>
        /// Shortcode can be used in WordPress in form <c>[viewName]</c>.
        /// </summary>
        /// <param name="app">WordPress request instance.</param>
        /// <param name="viewName">Name of the shared partial view. Also name of the registered shortcode.</param>
        /// <param name="shortCodeName">Name for the short code.</param>
        /// <param name="viewModelFact">Optional function that provides model for the view. The input parameter is a dictionary of shortcode arguments provided by WordPress.</param>
        public static void RegisterPartialViewAsShortcode(
            this WpApp app,
            string viewName,
            string shortCodeName,
            Func<IDictionary<IntStringKey, PhpValue>, object?>? viewModelFact = null)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            //
            app.AddShortcode(shortCodeName ?? viewName, new shortcode_handler((attrs, _, _) =>
            {
                //
                object? viewModel = viewModelFact?.Invoke(attrs);

                return app.Context.Partial(viewName, viewModel);
            }));
        }

        /// <summary>
        /// Register shortcode that renders shared partial view (<c>.cshtml</c> file).
        /// <br/>
        /// Shortcode can be used in WordPress in form <c>[viewName]</c>.
        /// </summary>
        /// <param name="app">WordPress request instance.</param>
        /// <param name="viewName">Name of the shared partial view. Also name of the registered shortcode.</param>
        /// <param name="viewModelFact">Optional function that provides model for the view. The input parameter is a dictionary of shortcode arguments provided by WordPress.</param>
        public static void RegisterPartialViewAsShortcode(
            this WpApp app,
            string viewName,
            Func<IDictionary<IntStringKey, PhpValue>, object?>? viewModelFact = null)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            RegisterPartialViewAsShortcode(
                app,
                viewName,
                shortCodeName: viewName.Replace('/', '_').Replace('\\', '_'),
                viewModelFact: viewModelFact
            );
        }

    }
}
