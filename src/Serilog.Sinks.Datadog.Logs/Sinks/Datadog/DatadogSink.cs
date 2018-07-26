// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogSink : PeriodicBatchingSink
    {
        private readonly string _apiKey;
        private readonly DatadogClient _client;

        /// <summary>
        /// API Key / message-content delimiter.
        /// </summary>
        private const string WhiteSpace = " ";

        /// <summary>
        /// Message delimiter.
        /// </summary>
        private const string MessageDelimiter = "\n";

        /// <summary>
        /// Shared JSON formatter.
        /// </summary>
        private static readonly JsonFormatter formatter = new JsonFormatter();

        /// <summary>
        /// Shared source used by Datadog logs-backend to normalize logs.
        /// </summary>
        private static readonly LogEventProperty Source = new LogEventProperty("ddsource", new ScalarValue("csharp"));

        /// <summary>
        /// The time to wait before emitting a new event batch.
        /// </summary>
        private static readonly TimeSpan Period = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The maximum number of events to emit in a single batch.
        /// </summary>
        private const int BatchSizeLimit = 100;

        public DatadogSink(string apiKey, DatadogConfiguration config) : base(BatchSizeLimit, Period)
        {
            _apiKey = apiKey;
            _client = new DatadogClient(config);
        }

        /// <summary>
        /// Emit a batch of log events to Datadog logs-backend.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            if (!events.Any())
            {
                return;
            }

            var payload = new StringBuilder();
            var writer = new StringWriter(payload);
            foreach (var logEvent in events)
            {
                payload.Append(_apiKey + WhiteSpace);
                logEvent.AddOrUpdateProperty(Source);
                formatter.Format(logEvent, writer);
                payload.Append(MessageDelimiter);
            }

            _client.write(payload.ToString());
        }

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            _client.close();
            base.Dispose(disposing);
        }
    }
}
