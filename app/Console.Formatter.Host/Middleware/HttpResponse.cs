using Custom.Logging.Abstractions;

namespace Console.Formatter.Host.Middleware
{
   public class HttpResponse : ILogContent
   {
      public string Url { get; set; }
      
      public int StatusCode { get; set; }
   }
}