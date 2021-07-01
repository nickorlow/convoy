using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Convoy.ErrorHandling
{
    public class Middleware
    {
        private readonly RequestDelegate _next;
        private readonly ConvoyErrorHandlingMiddlewareOptions _options;
        
        public Middleware(RequestDelegate next, ConvoyErrorHandlingMiddlewareOptions options)
        {
            _next = next;
            _options = options;
        }
        
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (ConvoyException ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            ConvoyException convoyException =
                new ConvoyException($"Internal Server Error", HttpStatusCode.InternalServerError);
            
            if(_options.LogInternalServerErrors || _options.LogAllErrors)
                LogErrorAsync(context, exception);
            
            return HandleExceptionAsync(context, convoyException, false);
        }
        
        private Task HandleExceptionAsync(HttpContext context, ConvoyException exception, bool log = true)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)exception.StatusCode;
            
            // This is to prevent reverse engineering attacks
            if(exception.StatusCode == HttpStatusCode.Unauthorized || exception.StatusCode == HttpStatusCode.InternalServerError)
                Thread.Sleep(new Random((int) (((DateTime.UtcNow-new DateTime(1970, 1, 1)).TotalMilliseconds)%4.393)).Next(500,5000));
           
            if (log && _options.LogAllErrors)
                LogErrorAsync(context, exception);
            
            return context.Response.WriteAsync(JsonConvert.SerializeObject(exception));
        }

        /// <summary>
        /// Function that calls the supplied callback function
        /// </summary>
        /// <param name="context">Current HttpContext</param>
        /// <param name="exception">Exception that was thrown</param>
        /// <returns>Empty Task</returns>
        private async Task LogErrorAsync(HttpContext context, Exception exception)
        {
            if(_options.ErrorLoggingFunction != null)
                await _options.ErrorLoggingFunction(context, exception);
        }
        
    }

    /// <summary>
    /// Options for the Convoy ErrorHandling Middleware 
    /// </summary>
    public class ConvoyErrorHandlingMiddlewareOptions
    {
        /// <summary>
        /// Callback function to be called when an error is thrown
        /// </summary>
        public Func<HttpContext, Exception,Task> ErrorLoggingFunction { get; set; }
        
        /// <summary>
        /// If true, this logs all errors, not just 500 errors.
        /// Default false
        /// Overrides LogInternalServerErrors
        /// </summary>
        public bool LogAllErrors { get; set; }
        
        /// <summary>
        /// If true, logs 500 internal server errors
        /// Default true
        /// Overridden by LogAllErrors
        /// </summary>
        public bool LogInternalServerErrors { get; set; }
        
        /// <summary>
        /// Default ConvoyErrorHandlingMiddlewareOptions
        ///
        /// Values:
        /// ErrorLoggingFunction = null
        /// LogAllErrors = false
        /// LogInternalServerErrors = true
        /// </summary>
        public static ConvoyErrorHandlingMiddlewareOptions Default => new ConvoyErrorHandlingMiddlewareOptions();
        
        public ConvoyErrorHandlingMiddlewareOptions()
        {
            LogAllErrors = false;
            LogInternalServerErrors = true;
            ErrorLoggingFunction = null;
        }
    }

    public static class MiddlewareExtension
    {
        public static IApplicationBuilder UseConvoyErrorHandlingMiddleware(this IApplicationBuilder builder, ConvoyErrorHandlingMiddlewareOptions options = null)
        {
            return builder.UseMiddleware<Middleware>(options ?? ConvoyErrorHandlingMiddlewareOptions.Default);
        }
    } 
}