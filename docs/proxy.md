# Sending logs through an HTTP proxy

If your environment requires outbound HTTPS traffic to go through an HTTP proxy, you can inject a
pre-configured `HttpClient` into the sink via the `IDatadogClient` extension point that was added
in v0.5.4. The sink does not expose a first-class proxy setting, but `HttpClientHandler` provides
full `WebProxy` support, including authenticated proxies.

**Scope:** This approach applies to the **HTTP transport only** (the default, `UseTCP = false`).
The TCP transport (`UseTCP = true` in `DatadogConfiguration`) opens a raw `TcpClient` socket and
does not use `HttpClient`, so the workaround below has no effect when TCP is enabled.

## Basic proxy (no credentials)

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

## Authenticated proxy

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

## Limitations

| Transport | Proxy support |
|-----------|---------------|
| HTTP (default, `UseTCP = false`) | Fully supported via `HttpClientHandler.Proxy` as shown above |
| TCP (`UseTCP = true`) | **Not supported** — the TCP transport uses raw sockets and ignores the `HttpClient` configuration |
