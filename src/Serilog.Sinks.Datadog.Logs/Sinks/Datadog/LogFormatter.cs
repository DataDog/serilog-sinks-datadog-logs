// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.Text;
using System.IO;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs.Sinks.Datadog;

namespace Serilog.Sinks.Datadog.Logs
{
    public class LogFormatter
    {
        private readonly LogEventProperty _source;
        private readonly LogEventProperty _service;
        private readonly LogEventProperty _host;
        private readonly LogEventProperty _tags;
        private readonly bool _recycleResources;
        private const int _maxSize = 2 * 1024 * 1024 - 51;  // Need to reserve space for at most 49 "," and "[" + "]"
        private static readonly StringBuilder _payloadBuilder = new StringBuilder(_maxSize, _maxSize);
        /// <summary>
        /// Default source value for the serilog integration.
        /// </summary>
        private const string CSHARP = "csharp";

        /// <summary>
        /// Shared JSON formatter.
        /// </summary>
        private static readonly DatadogFormatter formatter = new DatadogFormatter(true);

        public LogFormatter(string source, string service, string host, string[] tags, bool recycleResources)
        {
            _source = new LogEventProperty("ddsource", new ScalarValue(source ?? CSHARP));
            _service = string.IsNullOrWhiteSpace(service) ? null : new LogEventProperty("ddservice", new ScalarValue(service));
            _host = string.IsNullOrWhiteSpace(host) ?  null : new LogEventProperty("ddhost", new ScalarValue(host));
            _tags = tags == null || tags.Length == 0 ? null : new LogEventProperty("ddtags", new ScalarValue(string.Join(",", tags)));
            _recycleResources = recycleResources;
            if (_recycleResources)
            {
                _payloadBuilder.Clear();
            }
        }

        /// <summary>
        /// formatMessage enrich the log event with DataDog metadata such as source, service, host and tags.
        /// </summary>
        public string FormatMessage(LogEvent logEvent)
        {
            var builder = _recycleResources ? _payloadBuilder : new StringBuilder();
            try
            {
                logEvent.AddPropertyIfAbsent(_source);
                if (_service != null) { logEvent.AddPropertyIfAbsent(_service); }
                if (_host != null) { logEvent.AddPropertyIfAbsent(_host); }
                if (_tags != null) { logEvent.AddPropertyIfAbsent(_tags); }
                var writer = new StringWriter(builder);
                // Serialize the event as JSON. The Serilog formatter handles the
                // internal structure of the logEvent to give a nicely formatted JSON
                formatter.Format(logEvent, writer);
                return builder.ToString();
            } finally
            {
                builder.Clear();
                // remove properties incase other sinks are being used.
                logEvent.RemovePropertyIfPresent("ddsource");
                logEvent.RemovePropertyIfPresent("ddservice");
                logEvent.RemovePropertyIfPresent("ddhost");
                logEvent.RemovePropertyIfPresent("ddtags");
            }
        }
    }
}
