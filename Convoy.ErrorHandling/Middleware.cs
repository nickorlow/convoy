using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Convoy.ErrorHandling
{
    public class Middleware
    {
        private readonly RequestDelegate _next;
        
        public Middleware(RequestDelegate next)
        {
            _next = next;
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
            
            LogErrorAsync(context, exception);
            
            return HandleExceptionAsync(context, convoyException, false);
        }
        
        private Task HandleExceptionAsync(HttpContext context, ConvoyException exception, bool log = true)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)exception.StatusCode;
            
            // This is to prevent reverse engineering attacks
            if(exception.StatusCode == HttpStatusCode.Unauthorized || exception.StatusCode == HttpStatusCode.InternalServerError)
                Thread.Sleep(new Random((int) (((DateTime.UtcNow-DateTime.UnixEpoch).TotalMilliseconds)%4.393)).Next(500,5000));
           
            if (log)
                LogErrorAsync(context, exception);
            
            return context.Response.WriteAsync(JsonConvert.SerializeObject(exception));
        }

        public async Task LogErrorAsync(HttpContext c, Exception e)
        {
            // TODO: let users pass their own function so we can call their db/err mon software
        }
        
    }
}