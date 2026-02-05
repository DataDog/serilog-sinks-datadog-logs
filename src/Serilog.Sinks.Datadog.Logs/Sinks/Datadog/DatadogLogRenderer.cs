// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System;
using Serilog.Events;
using System.Collections.Generic;
using System.Text;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using System.Linq;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogLogRenderer
    {
        private const string CSHARP = "csharp";
        private int _maxMessageSize;
        private readonly List<LogEventProperty> _props;
        private readonly ITextFormatter _formatter;
        private readonly byte[] _truncatedFlag = Encoding.UTF8.GetBytes("...TRUNCATED...");
        private readonly int _ddPayloadSize;

        public DatadogLogRenderer(string source, string service, string host, string[] tags, int maxMessageSize, ITextFormatter formatter, int? ddPayloadSize = null)
        {

            // Resolve values from environment variables when not provided
            var resolvedSource = string.IsNullOrWhiteSpace(source) ? (GetEnv("DD_SOURCE") ?? CSHARP) : source;
            var resolvedService = string.IsNullOrWhiteSpace(service) ? GetEnv("DD_SERVICE") : service;
            var resolvedHost = string.IsNullOrWhiteSpace(host) ? GetEnv("DD_HOST") : host;
            var resolvedTags = MergeWithDatadogEnvTags(tags);

            var props = new List<LogEventProperty> {
                new LogEventProperty("ddsource", new ScalarValue(resolvedSource)),
            };
            if (resolvedService != null) { props.Add(new LogEventProperty("service", new ScalarValue(resolvedService))); }
            if (resolvedHost != null) { props.Add(new LogEventProperty("host", new ScalarValue(resolvedHost))); }
            if (resolvedTags != null && resolvedTags.Length > 0) { props.Add(new LogEventProperty("ddtags", new ScalarValue(string.Join(",", resolvedTags)))); }
            _props = props;
            _maxMessageSize = maxMessageSize;
            _formatter = formatter;

            // We have to account for the size of the dd (wrapper) payload in order to accurately truncate the
            // message. These values are only set once at startup - so we can cache this size and re-use it. 
            _ddPayloadSize = ddPayloadSize ?? Encoding.UTF8.GetByteCount(ToDDPayload(""));
        }

        public string[] RenderDatadogEvents(LogEvent logEvent)
        {
            // Render the payload with the default (or user supplied) ITextFormatter
            var payload = new StringBuilder();
            var payloadWriter = new System.IO.StringWriter(payload);
            _formatter.Format(logEvent, payloadWriter);
            var rawPayload = payloadWriter.ToString();

            return TruncateIfNeeded(rawPayload)
                .Select(x => ToDDPayload(Encoding.UTF8.GetString(x)))
                .ToArray();
        }

        internal IEnumerable<byte[]> TruncateIfNeeded(string rawPayload)
        {

            // In order to ensure the message fits into the final payload - we need to account for
            // the size of the (possibly inserted) truncated flags + the size of the wrapping json.
            var maxSize = _maxMessageSize - (2 * _truncatedFlag.Count()) - _ddPayloadSize;

            var bytes = Encoding.UTF8.GetBytes(rawPayload);

            // Split the payload into chunks within the size constraints. 
            var grouped = Enumerable.Range(0, (bytes.Count() / maxSize) + 1)
                .Select((b, i) => bytes.Skip(i * maxSize)
                                       .Take(maxSize))
                .Where(x => x.Count() > 0);

            // If the payload was split, we have to append and prepend the `...TRUNCATED...` flag to the messages.
            if (grouped.Count() > 1)
            {
                var count = grouped.Count();
                grouped = grouped.Select((x, i) =>
                {
                    if (i == 0)
                    {
                        return x.Concat(_truncatedFlag);
                    }
                    else if (i == count - 1)
                    {
                        return _truncatedFlag.Concat(x);
                    }
                    return _truncatedFlag.Concat(x.Concat(_truncatedFlag));
                });
            }

            return grouped.Select(x => x.ToArray());
        }

        internal string ToDDPayload(string rawPayload)
        {
            return ToDDPayload(rawPayload, _props);
        }

        internal string ToDDPayload(string rawPayload, IReadOnlyList<LogEventProperty> props)
        {
            // Render the dd event - a private json structure with the user event in the `message` field and 
            // Datadog specific fields at the root level. The message field can accept any format. By default 
            // Serilog sink will emit json - but the user can change change this format. 
            var formatter = new JsonValueFormatter();
            var ddPayload = new StringBuilder();
            var ddPayloadWriter = new System.IO.StringWriter(ddPayload);

            ddPayloadWriter.Write("{");
            foreach (var prop in props)
            {
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

        private static string TryConvertScalarToString(LogEventPropertyValue value)
        {
            if (value is ScalarValue scalar && scalar.Value is string s)
            {
                return s;
            }
            return null;
        }

        private static string GetEnv(string name)
        {
            try
            {
                return Environment.GetEnvironmentVariable(name);
            }
            catch
            {
                return null;
            }
        }

        private static string[] MergeWithDatadogEnvTags(string[] originalTags)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (originalTags != null)
            {
                foreach (var t in originalTags)
                {
                    var trimmed = (t ?? "").Trim();
                    if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            var ddTags = GetEnv("DD_TAGS");
            if (!string.IsNullOrWhiteSpace(ddTags))
            {
                var parts = ddTags.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var trimmed = p.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            var ddEnv = GetEnv("DD_ENV");
            if (!string.IsNullOrWhiteSpace(ddEnv))
            {
                var envTag = $"env:{ddEnv}";
                if (seen.Add(envTag))
                {
                    result.Add(envTag);
                }
            }

            var ddVersion = GetEnv("DD_VERSION");
            if (!string.IsNullOrWhiteSpace(ddVersion))
            {
                var versionTag = $"version:{ddVersion}";
                if (seen.Add(versionTag))
                {
                    result.Add(versionTag);
                }
            }

            return result.ToArray();
        }
    }
}
