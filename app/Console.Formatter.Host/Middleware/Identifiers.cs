using System;
using Custom.Logging.Abstractions;

namespace Console.Formatter.Host.Middleware
{
   public class Identifiers : ILogScope
   {
      public Guid CorrelationId { get; set; }
      
      public string UserId { get; set; }
      
      public string CustomerId { get; set; }
   }
}