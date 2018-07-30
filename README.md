# Serilog.Sinks.Datadog.Logs

A Serilog sink that send events and logs staight away to Datadog. By default the sink will use a TCP connection over SSL.

**Package** - [Serilog.Sinks.Datadog.Logs](http://nuget.org/packages/serilog.sinks.datadog.logs)
| **Platforms** - .NET 4.5.1, netstandard1.3, netstandard2.0

```csharp
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();
```

You can override the default behavior by manually specifing the following properties (endpoint, port, useSSL).

```
var config = new DatadogConfiguration("intake.logs.datadoghq.com", 10516, true);
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>", config)
    .CreateLogger();
```

## Example

Sending the following log:

```
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();

// An example
var position = new { Latitude = 25, Longitude = 134 };
var elapsedMs = 34;

log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
```

In the platform, the log looks like as the following JSON Object:

```
{
    "MessageTemplate": "Processed {@Position} in {Elapsed:000} ms.",
    "Level": "Information",
    "Timestamp": "2016-09-02T15:02:29.648Z",
    "Renderings": {
        "Elapsed": [{
            "Format": "000",
            "Rendering": "034"
        }]
    },
    "Properties": {
        "Position": {
            "Latitude": 25,
            "Longitude": 134
        },
        "Elapsed": 34
    }
}
```
