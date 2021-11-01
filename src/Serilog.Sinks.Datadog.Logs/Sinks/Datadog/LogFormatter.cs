// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.Text;
using System.IO;
using System.Collections.Generic;
using Serilog.Events;
using Serilog.Formatting.Json;
#if NET5_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
#else
using Newtonsoft.Json;
#endif

namespace Serilog.Sinks.Datadog.Logs
{
    public class LogFormatter
    {
        private readonly string _source;
        private readonly string _service;
        private readonly string _host;
        private readonly string _tags;

        /// <summary>
        /// Default source value for the serilog integration.
        /// </summary>
        private const string CSHARP = "csharp";

        /// <summary>
        /// Shared JSON formatter.
        /// </summary>
        private static readonly JsonFormatter formatter = new JsonFormatter(renderMessage: true);

#if NET5_0_OR_GREATER
        /// <summary>
        /// Settings to drop null values.
        /// </summary>
        private static readonly JsonSerializerOptions settings = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
#else

        /// <summary>
        /// Settings to drop null values.
        /// </summary>
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Newtonsoft.Json.Formatting.None };
#endif

        public LogFormatter(string source, string service, string host, string[] tags)
        {
            _source = source ?? CSHARP;
            _service = service;
            _host = host;
            _tags = tags != null ? string.Join(",", tags) : null;
        }

        /// <summary>
        /// formatMessage enrich the log event with DataDog metadata such as source, service, host and tags.
        /// </summary>
        public string FormatMessage(LogEvent logEvent)
        {
            var payload = new StringBuilder();
            var writer = new StringWriter(payload);

            // Serialize the event as JSON. The Serilog formatter handles the
            // internal structure of the logEvent to give a nicely formatted JSON
            formatter.Format(logEvent, writer);

            // Convert the JSON to a dictionnary and add the DataDog properties
#if NET5_0_OR_GREATER

            var logEventAsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.ToString());
#else
            var logEventAsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload.ToString());
#endif

            if (_source != null) { logEventAsDict.Add("ddsource", _source); }
            if (_service != null) { logEventAsDict.Add("service", _service); }
            if (_host != null) { logEventAsDict.Add("host", _host); }
            if (_tags != null) { logEventAsDict.Add("ddtags", _tags); }

            // Rename serilog attributes to Datadog reserved attributes to have them properly
            // displayed on the Log Explorer
            RenameKey(logEventAsDict, "RenderedMessage", "message");
            RenameKey(logEventAsDict, "Level", "level");
            // Convert back the dict to a JSON string
#if NET5_0_OR_GREATER
    return JsonSerializer.Serialize(logEventAsDict, settings);
#else
            return JsonConvert.SerializeObject(logEventAsDict, settings);
#endif
        }

        /// <summary>
        /// Renames a key in a dictionary.
        /// </summary>
        private void RenameKey<TKey, TValue>(IDictionary<TKey, TValue> dict,
                                           TKey oldKey, TKey newKey)
        {
            if (dict.TryGetValue(oldKey, out TValue value))
            {
                dict.Remove(oldKey);
                dict.Add(newKey, value);
            }
        }
    }
}
