using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

// ReSharper disable once CheckNamespace
namespace Custom.Logging.Extensions
{
   public static class LoggingBuilderExtensions
   {
      public static void AddCustomConsole(this ILoggingBuilder loggingBuilder)
      {
         loggingBuilder.AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));

         loggingBuilder.Services.AddSingleton<ConsoleFormatter, CustomConsoleFormatter>();
      }
   }
}