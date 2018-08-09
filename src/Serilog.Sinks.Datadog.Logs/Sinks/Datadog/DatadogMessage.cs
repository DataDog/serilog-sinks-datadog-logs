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
        /// Log Event.
        /// </summary>
        [JsonProperty("log_event")]
        public LogEvent Event { get; private set; }

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
            Event = logEvent;
            Source = source;
            Service = service;
            Tags = tags;
        }

        /// <summary>
        /// Returns the JSON representation of the message.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None, settings);
        }
    }
}
