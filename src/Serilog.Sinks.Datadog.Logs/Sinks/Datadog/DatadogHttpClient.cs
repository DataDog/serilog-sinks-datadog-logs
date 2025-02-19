// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogHttpClient : IDatadogClient
    {
        private const string _content = "application/json";
        private const int _maxPayloadSize = 5 * 1000 * 1000;
        private const int _maxMessageCount = 1000;

        private readonly string _url;
        private readonly DatadogLogRenderer _renderer;
        private readonly HttpClient _client;
        private readonly int _maxRetries;

        /// <summary>
        /// Max backoff used when sending failed.
        /// </summary>
        private const int MaxBackoff = 30;

        public DatadogHttpClient(string url, DatadogLogRenderer renderer, HttpClient client, int maxRetries)
        {
            _url = url;
            _renderer = renderer;
            _client = client;
            _client.DefaultRequestHeaders.ConnectionClose = true;
            _maxRetries = maxRetries;
        }

        public Task WriteAsync(IEnumerable<LogEvent> events)
        {
            var builtEvents = BuildEvents(events);
            var tasks = builtEvents.Select(post => Post(post));
            return Task.WhenAll(tasks);
        }

        private List<JsonPayloadBuilder> BuildEvents(IEnumerable<LogEvent> events)
        {
            var builders = new List<JsonPayloadBuilder>();
            var builder = new JsonPayloadBuilder();

            foreach (var logEvent in events)
            {
                var payloads = _renderer.RenderDatadogEvents(logEvent);
                foreach (var payload in payloads)
                {
                    if (builder.Size()+Encoding.UTF8.GetByteCount(payload) >= _maxPayloadSize || builder.Count() >= _maxMessageCount)
                    {
                        builders.Add(builder);
                        builder = new JsonPayloadBuilder();
                    }
                    builder.Add(payload, logEvent);
                }
            }
            if (builder.Count() > 0)
            {
                builders.Add(builder);
            }

            return builders;
        }

        private async Task Post(JsonPayloadBuilder payloadBuilder)
        {
            var payload = payloadBuilder.Build();
            HttpResponseMessage lastResult = null;
            Exception lastException = null;
            for (int retry = 0; retry < _maxRetries; retry++)
            {
                int backoff = (int)Math.Min(Math.Pow(2, retry), MaxBackoff);
                if (retry > 0)
                {
                    await Task.Delay(backoff * 1000);
                }

                try
                {
                    // Certain older versions of .NET Core will automatically dispose of the Content object before PostAsync returns.
                    // To guarantee portability, recreate the StringContent every retry.
                    var result = await _client.PostAsync(_url, new StringContent(payload, Encoding.UTF8, _content));
                    lastResult = result;
                    
                    if (result == null) { continue; }
                    if ((int)result.StatusCode >= 500) { continue; }
                    if ((int)result.StatusCode == 429) { continue; }
                    if ((int)result.StatusCode >= 400) { break; }
                    if (result.IsSuccessStatusCode) { return; }
                }
                catch (Exception e)
                {
                    lastException = e;
                    continue;
                }
            }

            if (lastException is null)
            {
                throw new CannotSendLogEventException(payload, payloadBuilder.LogEvents, lastResult);
            }
            else
            {
                throw new CannotSendLogEventException(payload, payloadBuilder.LogEvents, lastException);
            }
        }

        void IDatadogClient.Close() { }
    }
}
