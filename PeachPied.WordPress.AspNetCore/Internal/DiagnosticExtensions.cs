using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PeachPied.WordPress.AspNetCore.Internal
{
    internal static class DiagnosticExtensions
    {
        public static readonly string InformationalVersion = typeof(DiagnosticExtensions).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        static string Logger = "Peachpie.Compiler.Diagnostics";
        static string LoggerType = "Observer";
        static string Module = "WpDotNet";

        [DebuggerNonUserCode]
        class Middleware
        {
            readonly RequestDelegate _next;
            readonly IObserver<object> _observer;

            public Middleware(RequestDelegate next, IObserver<object> observer)
            {
                _next = next;
                _observer = observer;

                //
                _observer.OnNext("wpdotnet/started");
            }

            [DebuggerNonUserCode]
            public async Task Invoke(HttpContext context)
            {
                try
                {
                    await _next.Invoke(context);
                }
                catch (Exception ex)
                {
                    _observer.OnError(ex);
                    throw;
                }
            }
        }

        static bool TryInitialize(out IObserver<object> observer)
        {
            observer = null;

            try
            {
                var ass = Assembly.Load(Logger);
                if (ass != null)
                {
                    var t = ass.GetType(Logger + "." + LoggerType, throwOnError: false);
                    if (t != null)
                    {
                        observer = Activator.CreateInstance(t, InformationalVersion, Module) as IObserver<object>;
                    }
                }
            }
            catch
            {

            }

            //
            return observer != null;
        }

        public static IApplicationBuilder UseDiagnostic(this IApplicationBuilder app)
        {
            return TryInitialize(out var observer)
                ? app.UseMiddleware<Middleware>(observer)
                : app;
        }
    }
}
