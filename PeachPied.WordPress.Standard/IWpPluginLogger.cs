using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.Standard
{
    /// <summary>
    /// Interface representing a WordPress plugin.
    /// </summary>
    public interface IWpPluginLogger
    {
        /// <summary>
        /// The log severity.
        /// </summary>
        public enum Severity
        {
            /// <summary>Informational message.</summary>
            Message,
            /// <summary>A warning.</summary>
            Warning,
            /// <summary>An error.</summary>
            Error,
        }

        /// <summary>Logs the message into the app' console.</summary>
        void Log(Severity severity, string message, Exception exception = null);
    }
}
