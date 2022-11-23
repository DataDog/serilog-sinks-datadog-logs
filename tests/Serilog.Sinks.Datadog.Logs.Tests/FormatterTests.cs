using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Datadog.Logs.Tests
{

    class MessageOnlyFormatterForTest : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            logEvent.RenderMessage(output);
        }
    }

    [TestFixture]
    public class FormatterTests
    {
        [Test]
        public void TestDefaultFormatter()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, logFormatter);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                var positions = new dynamic[]
                {
                    new { Latitude = byte.MinValue, Longitude = byte.MaxValue },
                    new { Latitude = short.MinValue, Longitude = short.MaxValue },
                    new { Latitude = int.MinValue, Longitude = int.MaxValue },
                    new { Latitude = long.MinValue, Longitude = long.MaxValue }
                };
                const int elapsedMs = 34;
                Assert.DoesNotThrow(() => log.Information("Processed {@Positions} in {Elapsed:000} ms.", new Dictionary<string, object>
                {
                    { "positions", positions },
                    { "creator", "ACME" }
                }, elapsedMs));
            }

            // Scrub the timestamp since this changes
            var scrubbed = Regex.Replace(noop.SentPayloads[0], "\\\"timestamp.*?,", "");
            Assert.AreEqual(@"{""ddsource"":""TEST"",""service"":""TEST"",""host"":""localhost"",""ddtags"":""the,coolest,test"",""message"":""{\\""message\"":\""Processed [(\\\""positions\\\"": [{ Latitude: 0, Longitude: 255 }, { Latitude: -32768, Longitude: 32767 }, { Latitude: -2147483648, Longitude: 2147483647 }, { Latitude: -9223372036854775808, Longitude: 9223372036854775807 }]), (\\\""creator\\\"": \\\""ACME\\\"")] in 034 ms.\"",\""MessageTemplate\"":\""Processed {@Positions} in {Elapsed:000} ms.\"",\""level\"":\""Information\"",\""Properties\"":{\""Positions\"":{\""positions\"":[{\""Latitude\"":0,\""Longitude\"":255},{\""Latitude\"":-32768,\""Longitude\"":32767},{\""Latitude\"":-2147483648,\""Longitude\"":2147483647},{\""Latitude\"":-9223372036854775808,\""Longitude\"":9223372036854775807}],\""creator\"":\""ACME\""},\""Elapsed\"":34},\""Renderings\"":[\""034\""]}""}", scrubbed);
        }

        [Test]
        public void TestDefaultCustomFormatter()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, new MessageOnlyFormatterForTest());
            var noop = new NoopClient(apiKey, logFormatter);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                var positions = new dynamic[]
                {
                    new { Latitude = byte.MinValue, Longitude = byte.MaxValue },
                    new { Latitude = short.MinValue, Longitude = short.MaxValue },
                    new { Latitude = int.MinValue, Longitude = int.MaxValue },
                    new { Latitude = long.MinValue, Longitude = long.MaxValue }
                };
                const int elapsedMs = 34;
                Assert.DoesNotThrow(() => log.Information("Processed {@Positions} in {Elapsed:000} ms.", new Dictionary<string, object>
                {
                    { "positions", positions },
                    { "creator", "ACME" }
                }, elapsedMs));
            }
            Assert.AreEqual(@"{""ddsource"":""TEST"",""service"":""TEST"",""host"":""localhost"",""ddtags"":""the,coolest,test"",""message"":""Processed [(\""positions\"": [{ Latitude: 0, Longitude: 255 }, { Latitude: -32768, Longitude: 32767 }, { Latitude: -2147483648, Longitude: 2147483647 }, { Latitude: -9223372036854775808, Longitude: 9223372036854775807 }]), (\""creator\"": \""ACME\"")] in 034 ms.""}", noop.SentPayloads[0]);
        }

        [Test]
        public void TestMaxLogLengthIsHandled()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, new MessageOnlyFormatterForTest());
            var noop = new NoopClient(apiKey, logFormatter);
            var exceptions = new List<Exception>();

            // Test a string that is just under the limit
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop, exceptionHandler: x => exceptions.Add(x)).CreateLogger())
            {
                 var str = new StringWriter();
                for (var i = 0; i < (256 * 1000); i++) {
                    str.Write("a");
                }
                log.Information(str.ToString());
            }
            Assert.IsEmpty(exceptions);

            // Test a string that is just under the limit
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop, exceptionHandler: x => exceptions.Add(x)).CreateLogger())
            {
                var str = new StringWriter();
                for (var i = 0; i < (256 * 1000) + 1; i++) {
                    str.Write("a");
                }
                log.Information(str.ToString());
            }
            Assert.AreEqual(exceptions[0].GetType(), typeof(TooBigLogEventException));
        }
    }
}