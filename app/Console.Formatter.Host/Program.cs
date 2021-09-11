using Custom.Logging;
using Custom.Logging.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Console.Formatter.Host
{
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      private static IHostBuilder CreateHostBuilder(string[] args) =>
         Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logBuilder =>
            {
               logBuilder.ClearProviders();
               logBuilder.AddCustomConsole();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
               webBuilder.UseStartup<Startup>();
            });
   }
}