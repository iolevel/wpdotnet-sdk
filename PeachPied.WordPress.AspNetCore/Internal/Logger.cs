using Microsoft.Extensions.Logging;
using PeachPied.WordPress.Standard;
using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    /// <summary>
    /// Implementation of <see cref="IWpPluginLogger"/> passing log message to <see cref="ILogger"/>.
    /// </summary>
    sealed class Logger : IWpPluginLogger
    {
        ILogger UnderlyingLogger { get; }

        public Logger(ILoggerFactory logger)
        {
            UnderlyingLogger = logger.CreateLogger("WordPress");
        }

        static LogLevel AsLogLevel(IWpPluginLogger.Severity severity) => severity switch
        {
            IWpPluginLogger.Severity.Message => LogLevel.Information,
            IWpPluginLogger.Severity.Warning => LogLevel.Warning,
            IWpPluginLogger.Severity.Error => LogLevel.Error,
            _ => LogLevel.Trace,
        };

        public void Log(IWpPluginLogger.Severity severity, string message, Exception exception = null)
        {
            UnderlyingLogger?.Log(AsLogLevel(severity), message);
        }
    }
}
