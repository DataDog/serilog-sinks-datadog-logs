// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

using System;
using System.Collections.Generic;
using Serilog.Events;
using Newtonsoft.Json;

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// DatadogMessage sent to logs-backend formatted in JSON.
    /// </summary>
    public class DatadogMessage
    {
        /// <summary>
        /// Message content.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        /// <summary>
        /// Message timestamp.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; private set; }

        /// <summary>
        /// Message severity level.
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; private set; }

        /// <summary>
        /// Message exception.
        /// </summary>
        [JsonProperty("exception")]
        public Exception Exception { get; private set; }

        /// <summary>
        /// Message properties.
        /// </summary>
        [JsonProperty("properties")]
        Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Integration name.
        /// </summary>
        [JsonProperty("ddsource")]
        public string Source { get; private set; }

        /// <summary>
        /// Service name.
        /// </summary>
        [JsonProperty("service")]
        public string Service { get; private set; }

        /// <summary>
        /// Custom tags.
        /// </summary>
        [JsonProperty("ddtags")]
        public string Tags { get; private set; }

        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        public DatadogMessage(LogEvent logEvent, string source, string service, string tags)
        {
            Message = logEvent.RenderMessage();
            Timestamp = logEvent.Timestamp;
            Level = logEvent.Level.ToString();
            Exception = logEvent.Exception;
            Source = source;
            Service = service;
            Tags = tags;

            if (logEvent.Properties != null) {
                Properties = new Dictionary<string, object>();
                foreach (var key in logEvent.Properties.Keys)
                {
                    var value = logEvent.Properties[key];
                    Properties.Add(key, value.ToString());
                }
            }
        }

        /// <summary>
        /// Returns the JSON representation of the message.
        /// </summary>
        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None, settings);
        }
    }
}
