// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog.Debugging;

namespace Serilog.Sinks.Datadog.Logs
{
    public sealed class DatadogHttpClient : IDatadogClient
    {

        private const string _version = "0.3.5";
        private const string _content = "application/json";
        private const int _maxSize = 2 * 1024 * 1024 - 51;  // Need to reserve space for at most 49 "," and "[" + "]"
        private const int _maxMessageSize = 256 * 1024;

        private readonly DatadogConfiguration _config;
        private readonly string _url;
        private readonly LogFormatter _formatter;
        private readonly HttpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;


        /// <summary>
        /// Max number of retries when sending failed.
        /// </summary>
        private const int MaxRetries = 10;

        /// <summary>
        /// Max backoff used when sending failed.
        /// </summary>
        private const int MaxBackoff = 30;

        public DatadogHttpClient(DatadogConfiguration config, LogFormatter formatter, string apiKey, CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _cancellationToken = _cancellationTokenSource.Token;
            _config = config;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("DD-API-KEY", apiKey);
            _client.DefaultRequestHeaders.Add("DD-EVP-ORIGIN", "Serilog.Sinks.Datadog.Logs");
            _client.DefaultRequestHeaders.Add("DD-EVP-ORIGIN-VERSION", _version);
            _url = $"{config.Url}/api/v2/logs";
            _formatter = formatter;
        }

        public async Task WriteAsync(LogEvent[] events, Action<Exception> onException)
        {
            var chunkCount = 0;
            var chunkStart = 0;

            var chunkBuilder = new StringBuilder(_maxSize);

            for (var i = 0; i < events.Length; i++)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                var formattedLog = _formatter.formatMessage(events[i]);
                var logSize = Encoding.UTF8.GetMaxByteCount(formattedLog.Length);
                if (logSize > _maxMessageSize)
                {
                    if (onException != null)
                    {
                        onException(new TooBigLogEventException(events[i]));
                    }
                    continue; // The log is dropped because the backend would not accept it
                }
                if ((chunkBuilder.Length + logSize) > _maxSize)
                {
                    var payload = chunkBuilder.Append(']').ToString();
                    var eventSegment = new ArraySegment<LogEvent>(events, chunkStart, chunkCount);
                    await Post(payload, eventSegment, onException).ConfigureAwait(false);
                    // Flush the chunkBuffer to the chunks and reset the chunkBuffer
                    chunkBuilder.Clear();
                    chunkCount = 0;
                    chunkStart = i;
                }
                // if the builder is empty write the prefix, otherwise write the delimiter
                chunkBuilder.Append(chunkBuilder.Length == 0 ? '[' : ',');
                // now write our formatted log
                chunkBuilder.Append(formattedLog);
                chunkCount++;
            }
        }

        private async Task Post(string payload, ArraySegment<LogEvent> events, Action<Exception> onException)
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                using (var content = new StringContent(payload, Encoding.UTF8, _content))
                {
                    for (var retry = 0; retry < MaxRetries; retry++)
                    {
                        var backoff = (int)Math.Min(Math.Pow(2, retry), MaxBackoff);
                        if (retry > 0)
                        {
                            await Task.Delay(backoff * 1000, _cancellationToken).ConfigureAwait(false);
                        }

                        try
                        {
                            using (var result = await _client.PostAsync(_url, content, _cancellationToken).ConfigureAwait(false))
                            {
                                if (result == null) { continue; }
                                if ((int)result.StatusCode >= 500) { continue; }
                                if ((int)result.StatusCode == 429) { continue; }
                                if ((int)result.StatusCode >= 400) { break; }
                                if (result.IsSuccessStatusCode) { return; }
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                if (events.Array != null && onException != null)
                {
                    var array = new LogEvent[events.Count];
                    Array.Copy(events.Array, events.Offset, array, 0, array.Length);
                    onException(new CannotSendLogEventException(payload, array));
                }
            }
        }

        void IDatadogClient.Close()
        {
            _cancellationTokenSource.Cancel();
            Dispose();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }
    }
}
