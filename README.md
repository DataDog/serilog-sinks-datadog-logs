# Serilog.Sinks.Datadog.Logs

A Serilog sink that send events and logs straight away to Datadog. By default the sink sends logs over HTTPS

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
    "message": "Processed { Latitude: 25, Longitude: 134 } in 034 ms.",
    "MessageTemplate": "Processed {@Position} in {Elapsed:000} ms.",
    "timestamp": "2022-11-23T09:48:56.0262350-05:00",
    "level": "Information",
    "Properties": {
        "Position": {
            "Latitude": 25,
            "Longitude": 134
        },
        "Elapsed": 34
    },
    "Renderings": [
        "034"
    ]
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

## Using Datadog DD_ environment variables

If sink arguments are not provided, the sink can fall back to the standard Datadog environment variables:

- `DD_SOURCE` → `ddsource` (defaults to `csharp` if not set)
- `DD_SERVICE` → `service`
- `DD_HOST` → `host`
- `DD_TAGS`, `DD_ENV`, `DD_VERSION` → merged into `ddtags`

Notes:
- `DD_TAGS` may be comma or space-separated. Example: `DD_TAGS="team:payments region:us-east-1"` or `DD_TAGS=a:1,b:2`
- `DD_ENV` and `DD_VERSION` are appended as `env:<value>` and `version:<value>` respectively
- An event-level `host` property still overrides the top-level host for that event

Example usage without explicitly passing arguments:

```bash
export DD_SERVICE="checkout-svc"
export DD_ENV="prod"
export DD_VERSION="1.2.3"
export DD_TAGS="team:payments,region:us-east-1"
export DD_HOST="web-01"
```

```csharp
using (var log = new LoggerConfiguration()
    // No source/service/host/tags passed; values come from DD_ env vars above
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger())
{
    log.Information("Started");
}
```

## Sending logs through an HTTP proxy

If your environment requires outbound HTTPS traffic to go through an HTTP proxy, you can inject a
pre-configured `HttpClient` into the sink via the `IDatadogClient` extension point that was added
in v0.5.4. The sink does not expose a first-class proxy setting, but `HttpClientHandler` provides
full `WebProxy` support, including authenticated proxies.

**Scope:** This approach applies to the **HTTP transport only** (the default, `UseTCP = false`).
The TCP transport (`UseTCP = true` in `DatadogConfiguration`) opens a raw `TcpClient` socket and
does not use `HttpClient`, so the workaround below has no effect when TCP is enabled.

### Basic proxy (no credentials)

```csharp
using System;
using System.Net;
using System.Net.Http;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

// 1. Create an HttpClientHandler that routes through your proxy.
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy.example.com:8080"),
    UseProxy = true,
};

// 2. Wrap it in an HttpClient and add the required Datadog headers.
var httpClient = new HttpClient(handler);
httpClient.DefaultRequestHeaders.Add("DD-API-KEY", "<API_KEY>");
httpClient.DefaultRequestHeaders.Add("DD-EVP-ORIGIN", "Serilog.Sinks.Datadog.Logs");
httpClient.DefaultRequestHeaders.Add("DD-EVP-ORIGIN-VERSION", "0.5.4");

// 3. Wire the HttpClient into a DatadogHttpClient (IDatadogClient implementation).
var datadogConfig = new DatadogConfiguration(); // default: HTTPS to http-intake.logs.datadoghq.com
var renderer = new DatadogLogRenderer(
    source: "csharp",
    service: "<SERVICE_NAME>",
    host: "<HOST_NAME>",
    tags: Array.Empty<string>(),
    maxMessageSize: 256_000,
    formatter: new DatadogJsonFormatter());

var ddClient = new DatadogHttpClient(
    url: $"{datadogConfig.Url}/api/v2/logs",
    renderer: renderer,
    client: httpClient,
    maxRetries: 10);

// 4. Pass the pre-built IDatadogClient to the sink.
using var log = new LoggerConfiguration()
    .WriteTo.Sink(DatadogSink.Create(
        apiKey: "<API_KEY>",
        source: "csharp",
        service: "<SERVICE_NAME>",
        host: "<HOST_NAME>",
        tags: Array.Empty<string>(),
        config: datadogConfig,
        client: ddClient))
    .CreateLogger();

log.Information("This log goes through the proxy.");
```

### Authenticated proxy

If your proxy requires a username and password, pass a `NetworkCredential` to `WebProxy`:

```csharp
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy.example.com:8080")
    {
        Credentials = new NetworkCredential(
            userName: "<PROXY_USER>",
            password: "<PROXY_PASSWORD>")
    },
    UseProxy = true,
};
```

All other steps remain the same as in the basic example above.

### Limitations

| Transport | Proxy support |
|-----------|---------------|
| HTTP (default, `UseTCP = false`) | Fully supported via `HttpClientHandler.Proxy` as shown above |
| TCP (`UseTCP = true`) | **Not supported** — the TCP transport uses raw sockets and ignores the `HttpClient` configuration |


You can implement a [custom `ITextFormatter` ](https://github.com/serilog/serilog/blob/dev/src/Serilog/Formatting/ITextFormatter.cs)and pass it to the sink to change the format of your logs. This is useful if you want to add/remove/modify fields from the final JSON payload, or emit non-json logs to Datadog. 

There are several options for implementing custom formatters. The easiest way is to use [Serilog-expressions](https://github.com/serilog/serilog-expressions). Below is an example of a Serilog-expression `ITextFormatter` that drops the `MessageTemplate` field: 

```C#
public class DatadogJsonNoTemplateFormatter: ExpressionTemplate
{
    public DatadogJsonNoTemplateFormatter() : base(@"{ {
        Timestamp: @t,
        level: @l,
        message: @m, 
        Properties: {..@p},
        Renderings: @r}
    }") {}
}
```

If you cannot use Serilog-expressions due to framework compatibility - you can implement your own with `JsonValueFormatter` or a default implementation ex: [serilog-formatting-compact](https://github.com/serilog/serilog-formatting-compact)


## Support Configuration Options

[`DatadogLogs`](https://github.com/DataDog/serilog-sinks-datadog-logs/blob/master/src/Serilog.Sinks.Datadog.Logs/Configuration/Extensions/System.Configuration/LoggerConfigurationDatadogLogsExtensions.cs#L40) supports the following arguments:

| argument                   | Type                   | Description                                                                                                                  |
| -------------------------- | ---------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `apiKey`                   | `string`               | Your Datadog API key.                                                                                                        |
| `source`                   | `string`               | The integration name.                                                                                                        |
| `service`                  | `string`               | The service name.                                                                                                            |
| `host`                     | `string`               | The host name.                                                                                                               |
| `tags`                     | `string[]`             | Custom tags.                                                                                                                 |
| `configuration`            | `DatadogConfiguration` | The Datadog logs client configuration.                                                                                       |
| `restrictedToMinimumLevel` | `LogEventLevel`        | The minimum log level for the sink. Takes precedence over `logLevel` when both are set.                                      |
| `logLevel`                 | `LogEventLevel`        | Legacy parameter to set the minimum log level for the sink. Used only if `restrictedToMinimumLevel` is not set.              |
| `batchSizeLimit`           | `int`                  | The maximum number of events to emit in a single batch.                                                                      |
| `batchPeriod`              | `TimeSpan`             | The time to wait before emitting a new event batch.                                                                          |
| `queueLimit`               | `int`                  | Maximum number of events to hold in the sink's internal queue, or `null` for an unbounded queue. The default is `100000`      |
| `exceptionHandler`         | `Action<Exception>`    | This function is called when an exception occurs when using `DatadogConfiguration.UseTCP=false` (the default configuration). |
| `detectTCPDisconnection`   | `bool`                 | Detect when the TCP connection is lost and recreate a new connection.                                                        |
| `formatter`                | `ITextFormatter`       | A custom formatter implementation to change the format of the logs                                                           |
| `maxMessageSize`           | `int`                  | The maximum size in bytes of a message before it is split into chunks                                                        |

**NOTE:** if `maxMessageSize` [exceeds the documented API limit of 1MB](https://docs.datadoghq.com/api/latest/logs/) - any payloads larger than 1MB will be dropped by the intake. 

## How to build the NuGet package

1. Update the version in `src/Serilog.Sinks.Datadog.Logs.csproj`.
2. Update the version in `src/Serilog.Sinks.Datadog.Logs/Sinks/Datadog/Consts.cs`.
3. Add a new entry in `CHANGELOG.md` describing the changes.
4. Push a tag if needed.
5. Run `./build-package.sh`.

You can find the `.nupkg` file at `src/Serilog.Sinks.Datadog.Logs/bin/Release/Serilog.Sinks.Datadog.Logs.<version>.nupkg`

## Serilog.Sinks.Async	
As `Serilog.Sinks.Datadog.Logs` implements [Serilog.Sinks.PeriodicBatching](https://github.com/serilog/serilog-sinks-periodicbatching), using [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) is not recommended.	

From [Serilog.Sinks.Async documentation](https://github.com/serilog/serilog-sinks-async#serilogsinksasync---):	
> Note: many of the network-based sinks (CouchDB, Elasticsearch, MongoDB, Seq, Splunk...) already perform asynchronous batching natively and do not benefit from this wrapper.


## Development Environment 
This repository includes a devcontainer configuration for VS Code. To use it:

1. Install VS Code and Docker.
2. Open the project in VS Code with the Dev Containers extension.

VS Code will build and start the container automatically.

The devcontainer includes:
- .NET SDK 8.0
- Mono for .NET Framework support
- VS Code debugging configurations for .NET Core and Mono

To test changes:
- Run the tests in the test explorer.
- Use the TestApp to send real logs or debug changes.
