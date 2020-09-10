// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogSink : PeriodicBatchingSink
    {
        private readonly IDatadogClient _client;

        /// <summary>
        /// The time to wait before emitting a new event batch.
        /// </summary>
        private static readonly TimeSpan DefaultBatchPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The maximum number of events to emit in a single batch.
        /// </summary>
        private const int DefaultBatchSizeLimit = 50;

        public DatadogSink(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config, int? batchSizeLimit = null, TimeSpan? batchPeriod = null, Action<Exception> exceptionHandler = null)
            : base(batchSizeLimit ?? DefaultBatchSizeLimit, batchPeriod ?? DefaultBatchPeriod)
        {
            _client = CreateDatadogClient(apiKey, source, service, host, tags, config, exceptionHandler);
        }

        public DatadogSink(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config, int queueLimit, int? batchSizeLimit = null, TimeSpan? batchPeriod = null, Action<Exception> exceptionHandler = null)
            : base(batchSizeLimit ?? DefaultBatchSizeLimit, batchPeriod ?? DefaultBatchPeriod, queueLimit)
        {
            _client = CreateDatadogClient(apiKey, source, service, host, tags, config, exceptionHandler);
        }

        public static DatadogSink Create(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config, int? batchSizeLimit = null, TimeSpan? batchPeriod = null, int? queueLimit = null, Action<Exception> exceptionHandler = null)
        {
            if (queueLimit.HasValue)
                return new DatadogSink(apiKey, source, service, host, tags, config, queueLimit.Value, batchSizeLimit, batchPeriod, exceptionHandler);

            return new DatadogSink(apiKey, source, service, host, tags, config, batchSizeLimit, batchPeriod, exceptionHandler);
        }

        /// <summary>
        /// Emit a batch of log events to Datadog logs-backend.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (!events.Any())
            {
                return;
            }

            await _client.WriteAsync(events).ConfigureAwait(false);
        }

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            _client.Close();
            base.Dispose(disposing);
        }

        private static IDatadogClient CreateDatadogClient(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration configuration, Action<Exception> exceptionHandler)
        {
            var logFormatter = new LogFormatter(source, service, host, tags);
            if (configuration.UseTCP)
            {
                return new DatadogTcpClient(configuration, logFormatter, apiKey);
            }
            else
            {
                return new DatadogHttpClient(configuration, logFormatter, apiKey, exceptionHandler);
            }
        }
    }
}
