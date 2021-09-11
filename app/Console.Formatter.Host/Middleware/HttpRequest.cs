using Custom.Logging.Abstractions;

namespace Console.Formatter.Host.Middleware
{
   public class HttpRequest : ILogContent
   {
      public string Url { get; set; }
   }
}