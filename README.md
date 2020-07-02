# Serilog.Sinks.Datadog.Logs

A Serilog sink that sends events and logs directly to Datadog. By default the sink sends logs over HTTPS.

**Package** - [Serilog.Sinks.Datadog.Logs](http://nuget.org/packages/serilog.sinks.datadog.logs)
| **Platforms** - .NET 4.5.1, netstandard1.3, netstandard2.0

Note: For other .NET versions, ensure that the default TLS version used is `1.2`

```csharp
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();
```
## Configuration

By default the logs are forwarded to Datadog via **HTTPS** on port 443 to the US site: `https://http-intake.logs.datadoghq.com)`.  
You can change the site to EU by using the `Url` property on the `DatadogConfiguration` object and set it to `https://http-intake.logs.datadoghq.eu`. This can be done by passing in the url in the constructor, or by setting the property directly.

You can override the default behavior and use **TCP** forwarding by manually specifing the following properties (url, port, useSSL, useTCP).

You can also add the following properties to the Serilog sink:  
 - source
 - service
 - host
 - tags

Here is an example where the logger is configured to use TCP and includes the optional properties mentioned above:

```csharp
var config = new DatadogConfiguration(url: "intake.logs.datadoghq.com", port: 10516, useSSL: true, useTCP: true);
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs(
        "<API_KEY>",
        source: "<SOURCE_NAME>",
        service: "<SERVICE_NAME>",
        host: "<HOST_NAME>",
        tags: new string[] {"<TAG_1>:<VALUE_1>", "<TAG_2>:<VALUE_2>"},
        configuration: config
    )
    .CreateLogger();
```

## Output

Sending the following log:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.DatadogLogs("<API_KEY>")
    .CreateLogger();
    
var position = new { Latitude = 25, Longitude = 134 };
var elapsedMs = 34;

log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
```

Results in the following JSON Object log entry when viewed in DataDog:

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

OBS!  
If a log message exceeds the max size allowed (defined as: `_maxSize = 2 * 1024 * 1024 - 51;`) the message is **not sent** to Datadog.

## Configuration from `appsettings.json`

Since 0.2.0, you can configure the Datadog sink by using an `appsettings.json` file with
the [Serilog.Setting.Configuration](https://github.com/serilog/serilog-settings-configuration) package.

In the `"Serilog.WriteTo"` array, add an entry for `DatadogLogs`. 

Example:

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
      }
    }
  ],
  "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
  "Properties": {
    "Application": "Sample"
  }
}
```

## Inner workings
The logger asynchronously emits events in batches that are set to run every 2 seconds. 10 retries are made, spaced out by an exponential retry using the following formula:  
`int backoff = (int)Math.Min(Math.Pow(2, retry), MaxBackoff);`  

`MaxBackOff` is set to 30. 

If the batch fails due to an exception (for example invalid url) the exception is swallowed and it keeps trying until there are no retries left. If none of the attempts have gone through it will log to the SelfLog (seriLog feature) using `SelfLog.WriteLine`. 

Call SelfLog.Enable() at program startup to enable the log.  

Example using Debug.WriteLine:

```
// assume logger setup from earlier example

Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));

log.Information("New ping");
// output

2020-06-30T18:56:07.6409072Z Could not send payload to Datadog: [{"Timestamp":"2020-06-30T20:56:07.2296983+02:00","level":"Information","MessageTemplate":"New ping","message":"New ping","ddsource":"csharp","service":"myService"}]
```
The exception is not logged.

#### Important note 
due to the periodic batching and exponential backoff retries *an entry might get lost if the application closes or restarts before the batch has been emitted, or retries attempts have not been exhausted.*  

If you want to test the logging in a console application make sure to add a delay that allows enough time to process the request. This can be done by using , for example, Thread.Sleep(time) or Task.Delay(time). Blocking the thread by using Thread.Sleep is generally not recommended, but can be used if you just want to verify that the configuration works.

## How to build the NuGet package

Bump the version in `src/Serilog.Sinks.Datadog.Logs.csproj` and merge your branch

Run these commands in the project root folder

```bash
git checkout master && git pull
msbuild Serilog.Sinks.Datadog.Logs.sln /t:restore /p:Configuration=Release
msbuild Serilog.Sinks.Datadog.Logs.sln /t:pack /p:Configuration=Release
```

You can find the `.nupkg` file at `src/Serilog.Sinks.Datadog.Logs/bin/Release/Serilog.Sinks.Datadog.Logs.<version>.nupkg`
