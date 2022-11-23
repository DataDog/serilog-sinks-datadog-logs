// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Events;
using System.Collections.Generic;
using System.Text;
using Serilog.Parsing;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogLogRenderer
    {
        private const string CSHARP = "csharp";
        private const int _maxMessageSize = 256 * 1000;
        private readonly List<LogEventProperty>  _props;
        private readonly ITextFormatter _formatter;

        public DatadogLogRenderer(string source, string service, string host, string[] tags, ITextFormatter formatter)
        {

            var props = new List<LogEventProperty> {
                new LogEventProperty("ddsource", new ScalarValue(source ?? CSHARP)),
            };
            if (service != null) { props.Add(new LogEventProperty("service", new ScalarValue(service))); }
            if (host != null) { props.Add(new LogEventProperty("host", new ScalarValue(host))); }
            if (tags != null) { props.Add(new LogEventProperty("ddtags", new ScalarValue(string.Join(",", tags)))); }
            _props = props;
            _formatter = formatter;
        }

        public string RenderDatadogEvent(LogEvent logEvent) {

            // Render the payload with the default (or user supplied) ITextFormatter
            var payload = new StringBuilder();
            var payloadWriter = new System.IO.StringWriter(payload);
            _formatter.Format(logEvent, payloadWriter);
            var rawPayload = payloadWriter.ToString();

            var rawLogSize = Encoding.UTF8.GetByteCount(rawPayload);
            if (rawLogSize > _maxMessageSize) {
                throw new TooBigLogEventException(new List<LogEvent>{ logEvent });
            }

            // Render the dd event - a private json structure with the user event in the `message` field and 
            // Datadog specific fields at the root level. The message field can accept any format. By default 
            // Serilog sink will emit json - but the user can change change this format. 
            var formatter = new JsonValueFormatter();
            var ddPayload = new StringBuilder();
            var ddPayloadWriter = new System.IO.StringWriter(ddPayload);

            ddPayloadWriter.Write("{");
            foreach (var prop in _props) {
                JsonValueFormatter.WriteQuotedJsonString(prop.Name, ddPayloadWriter);
                ddPayloadWriter.Write(":");
                formatter.Format(prop.Value, ddPayloadWriter);
                ddPayloadWriter.Write(",");
            }
            JsonValueFormatter.WriteQuotedJsonString("message", ddPayloadWriter);
            ddPayloadWriter.Write(":");
            JsonValueFormatter.WriteQuotedJsonString(rawPayload, ddPayloadWriter);

            ddPayloadWriter.Write("}");

            return ddPayloadWriter.ToString();
        }
    }
}
