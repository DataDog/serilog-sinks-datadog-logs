// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;
using Newtonsoft.Json;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogSink : PeriodicBatchingSink
    {
        private readonly string _apiKey;
        private readonly string _source;
        private readonly string _service;
        private readonly string _host;
        private readonly string _tags;
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
        /// The time to wait before emitting a new event batch.
        /// </summary>
        private static readonly TimeSpan Period = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Settings to drop null values.
        /// </summary>
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        /// <summary>
        /// The maximum number of events to emit in a single batch.
        /// </summary>
        private const int BatchSizeLimit = 100;

        public DatadogSink(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config) : base(BatchSizeLimit, Period)
        {
            _apiKey = apiKey;
            _source = source;
            _service = service;
            _host = host;
            _tags = tags != null ? string.Join(",", tags) : null;
            _client = new DatadogClient(config);
        }

        private string formatMessage(LogEvent logEvent, string source, string service, string host, string tags) {
            var payload = new StringBuilder();
            var writer = new StringWriter(payload);

            // Serialize the event as JSON. The Serilog formatter handles the
            // internal structure of the logEvent to give a nicely formatted JSON
            formatter.Format(logEvent, writer);

            // Convert the JSON to a dictionnary and add the DataDog properties
            var logEventAsDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(payload.ToString());
            logEventAsDict.Add("ddsource", source);
            logEventAsDict.Add("service", service);
            logEventAsDict.Add("host", host);
            logEventAsDict.Add("ddtags", tags);

            // Convert back the dict to a JSON
            return JsonConvert.SerializeObject(logEventAsDict, Newtonsoft.Json.Formatting.None, settings);
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

            var payload = new StringBuilder();
            foreach (var logEvent in events)
            {
                payload.Append(_apiKey + WhiteSpace);
                payload.Append(formatMessage(logEvent, _source, _service, _host, _tags));
                payload.Append(MessageDelimiter);
            }

            await _client.WriteAsync(payload.ToString());
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
    }
}
