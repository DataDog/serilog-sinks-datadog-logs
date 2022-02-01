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
        private readonly ILogFormatter _formatter;

        public string LastLog { get; private set; }

        public NoopClient(string apiKey, ILogFormatter formatter)
        {
            _apiKey = apiKey;
            _formatter = formatter;
        }

        public Task WriteAsync(IEnumerable<LogEvent> events)
        {


            var payloadBuilder = new StringBuilder();
            Assert.DoesNotThrow(() => {

                foreach (var logEvent in events)
                {
                    payloadBuilder.Append(_apiKey).Append(' ');
                    var formatted = _formatter.FormatMessage(logEvent);
                    Assert.IsNotEmpty(formatted);
                    payloadBuilder.Append(formatted);
                    payloadBuilder.Append('\n');
                }
            });
            var payload = payloadBuilder.ToString();
            Assert.IsNotEmpty(payload);
            LastLog = payload;

            return Task.CompletedTask;
        }

        public void Close()
        {

        }
    }
}
