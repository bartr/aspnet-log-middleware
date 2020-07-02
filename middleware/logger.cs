using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Middleware
{
    /// <summary>
    /// Simple aspnet core middleware that logs requests to the console
    /// </summary>
    public class Logger
    {

        // next action to Invoke
        private readonly RequestDelegate next;
        private readonly LoggerOptions options;

        private const string ipHeader = "X-Client-IP";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="next">RequestDelegate</param>
        /// <param name="options">LoggerOptions</param>
        public Logger(RequestDelegate next, IOptions<LoggerOptions> options)
        {
            // save for later
            this.next = next;
            this.options = options?.Value;

            if (this.options == null)
            {
                // use default
                this.options = new LoggerOptions();
            }
        }

        /// <summary>
        /// Called by aspnet pipeline
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <returns>Task (void)</returns>
        public async Task Invoke(HttpContext context)
        {
            // set start time
            DateTime dtStart = DateTime.Now;

            // Invoke next handler
            if (next != null)
            {
                await next.Invoke(context).ConfigureAwait(false);
            }

            // compute request duration
            var duration = DateTime.Now.Subtract(dtStart).TotalMilliseconds;

            // don't log favicon.ico 404s
            if (context.Request.Path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // write the results to the console
            if (ShouldLogRequest(context.Response))
            {
                Console.WriteLine($"{context.Response.StatusCode}\t{duration,6:0}\t{context.Request.Headers[ipHeader]}\t{GetPathAndQuerystring(context.Request)}");
            }
        }

        /// <summary>
        /// Check log level to determine if request should be logged
        /// </summary>
        /// <param name="response">HttpResponse</param>
        /// <returns></returns>
        private bool ShouldLogRequest(HttpResponse response)
        {

            // check for logging by response level
            if (response.StatusCode < 300)
            {
                if (!options.Log2xx)
                {
                    return false;
                }
            }
            else if (response.StatusCode < 400)
            {
                if (!options.Log3xx)
                {
                    return false;
                }
            }
            else if (response.StatusCode < 500)
            {
                if (!options.Log4xx)
                {
                    return false;
                }
            }
            else
            {
                if (!options.Log5xx)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Return the path and query string if it exists
        /// </summary>
        /// <param name="request">HttpRequest</param>
        /// <returns>string</returns>
        private static string GetPathAndQuerystring(HttpRequest request)
        {
            return request?.Path.ToString() + request?.QueryString.ToString();
        }
    }
}
