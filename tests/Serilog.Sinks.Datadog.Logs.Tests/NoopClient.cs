using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Serilog.Events;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    /// <summary>
    /// A mock <see cref="IDatadogClient"/> that formats and writes logs to nowhere for the purpose of testing.
    /// </summary>
    public class NoopClient : IDatadogClient
    {
        private readonly string _apiKey;
        private readonly DatadogLogRenderer _formatter;
        public List<string> SentPayloads = new List<string>();

        public NoopClient(string apiKey, DatadogLogRenderer formatter)
        {
            _apiKey = apiKey;
            _formatter = formatter;
        }

        public Task WriteAsync(IEnumerable<LogEvent> events)
        {

            foreach (var logEvent in events)
            {
                var payloads = _formatter.RenderDatadogEvents(logEvent);
                foreach (var payload in payloads) 
                {
                    SentPayloads.Add(payload);
                }
            }
            return Task.CompletedTask;
        }

        public void Close()
        {

        }
    }
}
