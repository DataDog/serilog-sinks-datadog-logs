using System;
using System.Collections.Generic;
using System.Runtime;
using NUnit.Framework;
using Serilog.Events;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class FormatterTests
    {
        [Test]
        public void CanFormat()
        {
            var exception = new Exception("Top", new Exception("Middle", new ApplicationException("Bottom")));
            var properties = new[]
            {
                new LogEventProperty("A", new ScalarValue(1)), new LogEventProperty("B", new DictionaryValue(new[]
                {
                    new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(1), new ScalarValue(2))
                }))
            };


            var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, exception, MessageTemplate.Empty, properties);

            var formatter = new LogFormatter(null, "TEST", "localhost", new[] { "the", "coolest", "test" });
            var message = formatter.FormatMessage(logEvent);
            Assert.That(!string.IsNullOrWhiteSpace(message));
        }
    }
}