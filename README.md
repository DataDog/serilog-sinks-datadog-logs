# Serilog.Sinks.Datadog.Logs

A Serilog sink that send events and logs staight away to Datadog. By default the sink sends logs over HTTPS

**Package** - [Serilog.Sinks.Datadog.Logs](http://nuget.org/packages/serilog.sinks.datadog.logs)
| **Platforms** - .NET 4.5, .NET 4.6.1, .NET 4.7.2, netstandard1.3, netstandard2.0

Note: For other .NET versions, ensure that the default TLS version used is `1.2`

```csharp
using (var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger())
{
    // Some code
}
```

By default the logs are forwarded to Datadog via **HTTPS** on port 443 to the US site.
You can change the site to EU by using the `url` property and set it to `https://http-intake.logs.datadoghq.eu`.

You can override the default behavior and use **TCP** forwarding by manually specifing the following properties (url, port, useSSL, useTCP).

You can also add the following properties (source, service, host, tags) to the Serilog sink.

* Example with a TCP forwarder which add the source, service, host and a list of tags to the logs:

```csharp
var config = new DatadogConfiguration(url: "intake.logs.datadoghq.com", port: 10516, useSSL: true, useTCP: true);
using (var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs(
        "<API_KEY>",
        source: "<SOURCE_NAME>",
        service: "<SERVICE_NAME>",
        host: "<HOST_NAME>",
        tags: new string[] {"<TAG_1>:<VALUE_1>", "<TAG_2>:<VALUE_2>"},
        configuration: config
    )
    .CreateLogger())
{
    // Some code
}
```

## Example

Sending the following log:

```csharp
using (var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger())
{    
    // An example
    var position = new { Latitude = 25, Longitude = 134 };
    var elapsedMs = 34;

    log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
}
```
or
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();
    
// An example
var position = new { Latitude = 25, Longitude = 134 };
var elapsedMs = 34;

Log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
Log.CloseAndFlush();
```

In the platform, the log looks like as the following JSON Object:

```json
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

## Configuration from `appsettings.json`

Since 0.2.0, you can configure the Datadog sink by using an `appsettings.json` file with
the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package.

In the `"Serilog.WriteTo"` array, add an entry for `DatadogLogs`. An example is shown below:

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Datadog.Logs" ],
  "MinimumLevel": "Debug",
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "DatadogLogs",
      "Args": {
        "apiKey": "<API_KEY>",
        "source": "<SOURCE_NAME>",
        "host": "<HOST_NAME>",
        "tags": ["<TAG_1>:<VALUE_1>", "<TAG_2>:<VALUE_2>"],
        "configuration" : {
          "url": "intake.logs.datadoghq.com", 
          "port": 10516, 
          "useSSL": true, 
          "useTCP": true
        }
      }
    }
  ],
  "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
  "Properties": {
    "Application": "Sample"
  }
}
```

**NOTE:** the `configuration` section is optional so that you may override the defaults. 

## Support Configuration Options

[`DatadogLogs`](https://github.com/DataDog/serilog-sinks-datadog-logs/blob/master/src/Serilog.Sinks.Datadog.Logs/Configuration/Extensions/System.Configuration/LoggerConfigurationDatadogLogsExtensions.cs#L40) supports the following arguments:

| argument                 | Type                   | Description                                             |
| ------------------------ | ---------------------- | ------------------------------------------------------- |
| `apiKey`                 | `string`               | Your Datadog API key.                                   |
| `source`                 | `string`               | The integration name.                                   |
| `service`                | `string`               | The service name.                                       |
| `host`                   | `string`               | The host name.                                          |
| `tags`                   | `string[]`             | Custom tags.                                            |
| `configuration`          | `DatadogConfiguration` | The Datadog logs client configuration.                  |
| `logLevel`               | `LogEventLevel`        | The minimum log level for the sink.                     |
| `batchSizeLimit`         | `int`                  | The maximum number of events to emit in a single batch. |
| `batchPeriod`            | `TimeSpan`             | The time to wait before emitting a new event batch.     |
| `queueLimit`             | `int`                  | Maximum number of events to hold in the sink's internal queue, or `null` for an unbounded queue. The default is `10000` |
| `exceptionHandler`       | `Action<Exception>`    | This function is called when an exception occurs when using `DatadogConfiguration.UseTCP=false` (the default configuration). |
| `detectTCPDisconnection` | `bool`                 | Detect when the TCP connection is lost and recreate a new connection. |

## How to build the NuGet package

Bump the version in `src/Serilog.Sinks.Datadog.Logs.csproj` and merge your branch

Run these commands in the project root folder

```bash
git checkout master && git pull
msbuild Serilog.Sinks.Datadog.Logs.sln /t:restore /p:Configuration=Release
msbuild Serilog.Sinks.Datadog.Logs.sln /t:pack /p:Configuration=Release
```

You can find the `.nupkg` file at `src/Serilog.Sinks.Datadog.Logs/bin/Release/Serilog.Sinks.Datadog.Logs.<version>.nupkg`

## Serilog.Sinks.Async	
As `Serilog.Sinks.Datadog.Logs` implements [Serilog.Sinks.PeriodicBatching](https://github.com/serilog/serilog-sinks-periodicbatching), using [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) is not recommended.	

From [Serilog.Sinks.Async documentation](https://github.com/serilog/serilog-sinks-async#serilogsinksasync---):	
> Note: many of the network-based sinks (CouchDB, Elasticsearch, MongoDB, Seq, Splunk...) already perform asynchronous batching natively and do not benefit from this wrapper.