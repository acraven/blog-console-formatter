using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Console.Formatter.Host.Middleware
{
   public class IdentifiersMiddleware
   {
      private readonly RequestDelegate _next;

      public IdentifiersMiddleware(RequestDelegate next)
      {
         _next = next;
      }
      
      public async Task Invoke(HttpContext context, ILogger<IdentifiersMiddleware> logger)
      {
         // Discover these from whatever context is available
         var scope = new Identifiers
         {
            CorrelationId = Guid.NewGuid(),
            UserId = "the-user",
            CustomerId = "the-customer",
         };
         
         // Identifiers will be logged by all other loggers in downstream middleware
         using (logger.BeginScope(scope))
         {
            await _next(context);
         }
      }
   }
}