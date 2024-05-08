// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogJsonFormatter : ITextFormatter
    {

        readonly JsonValueFormatter _valueFormatter = new JsonValueFormatter();
        public void Format(LogEvent logEvent, TextWriter output)
        {
            // Largely based on https://github.com/serilog/serilog-formatting-compact/blob/dev/src/Serilog.Formatting.Compact/Formatting/Compact/RenderedCompactJsonFormatter.cs
            // TODO: Replace with https://github.com/serilog/serilog-expressions if we ever drop the unsupported .net versions. 
            output.Write("{");

            writeKeyValue("timestamp", logEvent.Timestamp.ToString("O"), output);

            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            writeKeyValue("message", message, output);

            writeKeyValue("MessageTemplate", logEvent.MessageTemplate.ToString(), output);
            writeKeyValue("level", logEvent.Level.ToString(), output);

            if (logEvent.Exception != null)
            {
                writeKeyValue("Exception", logEvent.Exception.ToString(), output);
            }

            // Properties 
            JsonValueFormatter.WriteQuotedJsonString("Properties", output);
            output.Write(":{");

            var propCount = 0;
            foreach (var property in logEvent.Properties)
            {
                propCount++;
                writeKeyValue(property.Key, property.Value, output, propCount == logEvent.Properties.Count);
            }
            output.Write("}");

            // Renderings
            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            if (tokensWithFormat.Any())
            {
                output.Write(",\"Renderings\":[");
                var delim = "";
                foreach (var r in tokensWithFormat)
                {
                    output.Write(delim);
                    delim = ",";
                    var space = new StringWriter();
                    r.Render(logEvent.Properties, space);
                    JsonValueFormatter.WriteQuotedJsonString(space.ToString(), output);
                }
                output.Write(']');
            }

            output.Write("}");
        }


        private void writeProperty(LogEventProperty property, TextWriter output, bool isLast = false)
        {
            writeKeyValue(property.Name, property.Value, output, isLast);
        }
        private void writeKeyValue(string key, LogEventPropertyValue val, TextWriter output, bool isLast = false)
        {
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(":");
            _valueFormatter.Format(val, output);
            if (!isLast)
            {
                output.Write(",");
            }
        }

        private void writeKeyValue(string key, string val, TextWriter output, bool isLast = false)
        {
            JsonValueFormatter.WriteQuotedJsonString(key, output);
            output.Write(":");
            JsonValueFormatter.WriteQuotedJsonString(val, output);
            if (!isLast)
            {
                output.Write(",");
            }
        }
    }

}
