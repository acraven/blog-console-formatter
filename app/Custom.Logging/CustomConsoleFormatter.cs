using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Custom.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Custom.Logging
{
   public class CustomConsoleFormatter : ConsoleFormatter
   {
      public CustomConsoleFormatter() : base(nameof(CustomConsoleFormatter))
      {
      }

      public override void Write<TState>(
         in LogEntry<TState> logEntry,
         IExternalScopeProvider scopeProvider,
         TextWriter textWriter)
      {
         var logContent = logEntry.State as ILogContent;
         var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

         using var memoryStream = new MemoryStream();

         using (var writer = new Utf8JsonWriter(memoryStream))
         {
            writer.WriteStartObject();

            writer.WriteString("Timestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            if (logContent != null)
            {
               writer.WriteString("Type", logContent.GetType().Name);
               writer.WritePropertyName("Content");

               JsonSerializer.Serialize(writer, logContent, logContent.GetType());
            }
            else
            {
               writer.WritePropertyName("Content");
               writer.WriteStartObject();
               writer.WriteString("Message", message);
               writer.WriteEndObject();
            }

            WriteProperties(writer, logEntry.State);
            WriteScopes(writer, scopeProvider);

            writer.WriteEndObject();
            writer.Flush();
         }

         var json = Encoding.UTF8.GetString(memoryStream.ToArray());

         textWriter.WriteLine(json);
      }

      private static void WriteProperties<TState>(Utf8JsonWriter writer, TState state)
      {
         if (state is IReadOnlyCollection<KeyValuePair<string, object>> stateAsKeyValuePairs)
         {
            var properties =
               new Dictionary<string, object>(stateAsKeyValuePairs.Where(c => c.Key != "{OriginalFormat}"));

            if (properties.Any())
            {
               writer.WritePropertyName("Properties");

               JsonSerializer.Serialize(writer, properties);
            }
         }
      }

      private static void WriteScopes(Utf8JsonWriter writer, IExternalScopeProvider scopeProvider)
      {
         void WriteScope(object scope, object _)
         {
            if (scope is ILogScope logScope)
            {
               writer.WritePropertyName(logScope.GetType().Name);

               JsonSerializer.Serialize(writer, logScope, logScope.GetType());
            }
         }

         scopeProvider?.ForEachScope<object>(WriteScope, null);
      }
   }
}