# blog-console-formatter

## Background

I'm working for a client who, like many of us in the past, has created their own logging abstraction; where they differ though, is that there's no underlying logging framework like Serilog, log4net etc., just plain old Console.WriteLine writing JSON.

The services run in AWS ECR with the console output being captured by CloudWatch. The result is that the logs are searchable by field, very much the goal of a structured logging approach and from an operations point-of-view it works pretty well. The basic console logging shipped with ASP.NET Core 3.1 doesn't support this making its log output less valueable than it should be.

It's rare that one ever swaps out a logging provider, but it gives people comfort that they can if they wish. It also isolates any existing code from a change to the logging provider's API in the unlikely event that happened.

To be clear I'm not advocating writing a logging abstraction, Microsoft provide a perfectly good [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-5.0) abstraction in `Microsoft.Extensions.Logging.Abstractions` for third-party implementations. Of which, I would recommend using [Serilog](https://serilog.net/) from day 1 which supports structured logging out of the box.

The problem I'm facing is that in its current form it's not extensible; the fields that are logged in the JSON are defined in POCO classes in the logging library, which is two dependencies away from the actual Web API services, making it pretty inflexible. I have a requirement to add customer, user and entity IDs when loading and saving from and to DynamoDB.

It would make more sense for the structured logging POCOs to live at the same level as which they are used; however, this still means that one has to create a POCO to do any form of structured logging. For some additional context, the standard methods in the abstraction follow the pattern `void LogInfo(string message);` rather than the familiar `void LogInformation(string message, params object[] args);` found on the extension methods of Microsoft's `ILogger` which support structured logging. Nor does the custom abstraction support anything like `ILogger`s `BeginScope` method that allows arbitrary data to be passed into the logging implementation.

Not being one to perpetuate an anti-pattern, I set about looking at how the logging abstraction can be replaced. I'm constrained by the current output which operations use in their dashboards, alerting etc. which means I can't just drop in Serilog.

The first step to my goal is to introduce the Microsoft abstraction alongside the existing abstraction so either can be used and with perhaps the addition of some extra fields to the structured logs operations shouldn't be affected. The second step is to migrate usages of the old abstraction to the Microsoft abstraction, before finally removing it as step three.

In this post I'm going to demonstrate how to write structured log message as JSON to the console.

## Current behaviour

```cs
   logger.LogInfo($"Processing message '{messageId}'");
```

```json
{
   "Timestamp": "2021-09-10T11:37:22.793Z",
   "Level": "INFO",
   "Detail": {
      "Message":"Processing message 'cd9f2a80-d240-443b-b0d4-25f0fa3303ab'"
   }
}
```

```cs
   logger.Log("Outbound HTTP request", new OutboundHttpRequest { Url = "https://example.com/foo" });
```

```json
{
   "Timestamp": "2021-09-10T11:37:22.793Z",
   "Level": "INFO",
   "Outline": "Outbound HTTP request",
   "Detail": {
      "Url":"Processing message 'cd9f2a80-d240-443b-b0d4-25f0fa3303ab'"
   }
}
```

## JsonConsoleFormatter

With .NET 5 along came the option to perform structured logging in ASP.NET Core out-of-the-box; simply add the `ConfigureLogging` method to the host builder as part of the default Web API project. Owing to its lack of LTS status .NET 5 isn't an option, but we can still use the v5 release of the `Microsoft.Extensions.Logging.Console` which contains the same `AddJsonConsole` method.

```cs
   private static IHostBuilder CreateHostBuilder(string[] args) =>
      Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
         .ConfigureLogging(logBuilder =>
         {
            logBuilder.ClearProviders();
            logBuilder.AddJsonConsole();
         })
         .ConfigureWebHostDefaults(webBuilder =>
         {
            webBuilder.UseStartup<Startup>();
         });
```

Running up the default ASP.NET Core Web API project, a few log messages get written to the console as JSON, the first of which is as follows (formatted for clarity).

```json
{
   "EventId": 0,
   "LogLevel": "Information",
   "Category": "Microsoft.Hosting.Lifetime",
   "Message": "Now listening on: https://localhost:5001",
   "State": {
      "Message": "Now listening on: https://localhost:5001",
      "address": "https://localhost:5001",
      "{OriginalFormat}": "Now listening on: {address}"
   }
}
```

## Serilog.Formatting.Json

For balance, the `Serilog.AspNetCore` package can be added to the default Web API project instead using the `UseSerilog` method.

```cs
   private static IHostBuilder CreateHostBuilder(string[] args) =>
      Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
         .UseSerilog((context, configuration) =>
         {
            configuration.WriteTo.Console(new JsonFormatter());
         })
         .ConfigureWebHostDefaults(webBuilder =>
         {
            webBuilder.UseStartup<Startup>();
         });
```

This outputs the same log message as follows during startup. I prefer this as it contains less duplication, but it still doesn't contain the properties I'm looking to replicate.

```json
{
   "Timestamp": "2021-09-10T12:01:34.0729739+01:00",
   "Level": "Information",
   "MessageTemplate": "Now listening on: {address}",
   "Properties": {
      "address": "https://localhost:5001",
      "SourceContext": "Microsoft.Hosting.Lifetime"
   }
}
```

## Creating our CustomConsoleFormatter

If you examine the code in the `AddJsonConsole` method you will see it's calling `AddConsole` passing `json` to wire-up an already registered `JsonConsoleFormatter`. We need to do something similar, but we also need to register our formatter too, which we are doing in `AddCustomConsole` below. We could use the `AddConsoleFormatter` extension method but this requires us to define an Options class for a formatter; since this is for internal use only we don't need any additional customisation.

```cs
      public static void AddCustomConsole(this ILoggingBuilder loggingBuilder)
      {
         loggingBuilder.AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));

         loggingBuilder.Services.AddSingleton<ConsoleFormatter, CustomConsoleFormatter>();
      }
```

```cs
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
```

The `CustomConsoleFormatter` below is using the formatter in the `LogEntry<TState>` to convert the `TState` object being logged into a `string`. The `JsonConsoleFormatter` above does the same when outputing the `Message` property, but the Serilog formatter chooses not to, instead outputing the template. This is a crude example as it stands, offering less value than the standard `JsonConsoleFormatter`, but I now have a platform to begin customising my JSON logs.

```cs
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
         var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

         using var memoryStream = new MemoryStream();

         using (var writer = new Utf8JsonWriter(memoryStream))
         {
            writer.WriteStartObject();

            writer.WriteString("Timestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.WriteString("Message", message);

            writer.WriteEndObject();
            writer.Flush();
         }

         var json = Encoding.UTF8.GetString(memoryStream.ToArray());

         textWriter.WriteLine(json);
      }
```

Again, the output of the same startup log message is as follows.

```json
{
   "Timestamp":"2021-09-10T12:18:43.508Z",
   "Message":"Now listening on: https://localhost:5001"
}
```
