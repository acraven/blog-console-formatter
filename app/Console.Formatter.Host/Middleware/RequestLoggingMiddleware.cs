using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Custom.Logging.Extensions;

namespace Console.Formatter.Host.Middleware
{
   public class RequestLoggingMiddleware
   {
      private readonly RequestDelegate _next;

      public RequestLoggingMiddleware(RequestDelegate next)
      {
         _next = next;
      }
      
      public async Task Invoke(HttpContext context, ILogger<RequestLoggingMiddleware> logger)
      {
         logger.LogInformation(new HttpRequest
         {
            Url = context.Request.Path
         });
         
         // Error handling omitted for brevity
         await _next(context);

         logger.LogInformation(new HttpResponse
         {
            Url = context.Request.Path,
            StatusCode = context.Response.StatusCode
         });
      }
   }
}