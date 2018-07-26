# Serilog.Sinks.Datadog.Logs

Sends log events to Datadog logs-backend.

**Package** - [Serilog.Sinks.Datadog.Logs](http://nuget.org/packages/serilog.sinks.datadog.logs)
| **Platforms** - .NET 4.5.1, netstandard1.3, netstandard2.0

```csharp
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();
```
