using System;
using Microsoft.Extensions.Logging;
using Custom.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Custom.Logging.Extensions
{
   public static class LoggerExtensions
   {
      public static void LogInformation<TLogContent>(this ILogger logger, TLogContent logContent)
         where TLogContent : ILogContent
      {
         logger.Log(LogLevel.Information, 0, logContent, null, NoOpFormatter);
      }

      private static string NoOpFormatter<TLogDetail>(TLogDetail logDetail, Exception exception)
      {
         return null;
      }
   }
}