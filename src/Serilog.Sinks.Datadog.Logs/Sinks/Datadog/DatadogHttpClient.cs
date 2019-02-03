// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Serilog.Debugging;
using System.Net.Http;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogHttpClient : IDatadogClient
    {
        private readonly DatadogConfiguration _config;
        private readonly string _url;
        private const string _content = "application/json";
        private readonly LogFormatter _formatter;
        private HttpClient _client;
        private readonly Encoding utf8 = Encoding.UTF8;
        private const int _maxSize = 2 * 1024 * 1024 - 51;  // Need to reserve space for at most 49 "," and "[" + "]"
        private const int _maxMessageSize = 256 * 1024;

        /// <summary>
        /// Max number of retries when sending failed.
        /// </summary>
        private const int MaxRetries = 1;

        /// <summary>
        /// Max backoff used when sending failed.
        /// </summary>
        private const int MaxBackoff = 30;

        /// <summary>
        /// Shared UTF8 encoder.
        /// </summary>
        private static readonly UTF8Encoding UTF8 = new UTF8Encoding();

        public DatadogHttpClient(DatadogConfiguration config, LogFormatter formatter, string apiKey)
        {
            _config = config;
            _client = new HttpClient();
            _url = $"{config.Url}/v1/input/{apiKey}";
            _formatter = formatter;
        }

        public async Task WriteAsync(IEnumerable<LogEvent> events)
        {
            Encoding utf8 = Encoding.UTF8;

            List<string> bulks = new List<string>();

            int currentSize = 0;
            var payload = new StringBuilder();
            payload.Append("[");
            var joinHelper = new List<string>(events.Count());
            foreach (var logEvent in events)
            {
                var formattedLog = _formatter.formatMessage(logEvent);
                var logSize = utf8.GetByteCount(formattedLog);
                if (logSize > _maxMessageSize)
                {
                    continue;  // The log is dropped because the backend would not accept it
                }
                if (currentSize + logSize > _maxSize)
                {
                    // Flush the payload to bulks and reset everything
                    payload.Append(String.Join(",", joinHelper.ToArray()));
                    payload.Append("]");
                    bulks.Add(payload.ToString());
                    payload.Clear();
                    joinHelper = new List<string>(events.Count());
                    currentSize = 0;
                    payload.Append("[");
                }
                joinHelper.Add(formattedLog);
                currentSize += logSize;
            }
            payload.Append(String.Join(",", joinHelper.ToArray()));
            payload.Append("]");
            bulks.Add(payload.ToString());
            var tasks = bulks.Select(Post);

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task Post(string payload)
        {
            var content = new StringContent(payload, Encoding.UTF8, _content);
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                int backoff = (int)Math.Min(Math.Pow(retry, 2), MaxBackoff);
                if (retry > 0)
                {
                    await Task.Delay(backoff * 1000);
                }

                try
                {
                    SelfLog.WriteLine("Sending payload to Datadog: {0}", payload);
                    var result = await _client.PostAsync(_url, content);
                    SelfLog.WriteLine("Statuscode: {0}", result.StatusCode);
                    if (result.StatusCode >= 500) { continue; }
                    if (result.IsSuccessStatusCode || result.StatusCode >= 400) { return; }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            SelfLog.WriteLine("Could not send payload to Datadog: {0}", payload);
        }

        void IDatadogClient.Close() {}
    }
}