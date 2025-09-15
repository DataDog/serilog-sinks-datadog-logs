using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Datadog.Logs.Tests
{

    class MessageOnlyFormatterForTest : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            logEvent.RenderMessage(output);
        }
    }

    class ValueHolder
    {
        public string Value { get; set; }
    }

    [TestFixture]
    public class FormatterTests
    {
        [Test]
        public void TestDefaultFormatter()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, 256 * 1000, new DatadogJsonFormatter());
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
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, 256 * 1000, new MessageOnlyFormatterForTest());
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
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, 256 * 1000, new MessageOnlyFormatterForTest());
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


            noop = new NoopClient(apiKey, logFormatter);
            // Test a string that is just under the limit
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop, exceptionHandler: x => exceptions.Add(x)).CreateLogger())
            {
                var str = new StringWriter();
                for (var i = 0; i < (256 * 1000) + 1; i++) {
                    str.Write("a");
                }
                log.Information(str.ToString());
            }
            Assert.IsEmpty(exceptions);
            // should be truncated
            Assert.AreEqual(2, noop.SentPayloads.Count);
        }

        [Test]
        public void TestTruncate()
        {
            var maxSize = 10 + (2 * "...TRUNCATED...".Count());
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, maxSize, new MessageOnlyFormatterForTest(), 0);

            var truncated = logFormatter.TruncateIfNeeded("1234567890abcdefghij").Select(x => Encoding.UTF8.GetString(x)).ToArray();
            Assert.AreEqual("1234567890...TRUNCATED...", truncated[0]);
            Assert.AreEqual("...TRUNCATED...abcdefghij", truncated[1]);

            truncated = logFormatter.TruncateIfNeeded("1234567890abcdefghij*").Select(x => Encoding.UTF8.GetString(x)).ToArray();
            Assert.AreEqual("1234567890...TRUNCATED...", truncated[0]);
            Assert.AreEqual("...TRUNCATED...abcdefghij...TRUNCATED...", truncated[1]);
            Assert.AreEqual("...TRUNCATED...*", truncated[2]);

            truncated = logFormatter.TruncateIfNeeded("1234567890").Select(x => Encoding.UTF8.GetString(x)).ToArray();
            Assert.AreEqual("1234567890", truncated[0]);

            truncated = logFormatter.TruncateIfNeeded("1234").Select(x => Encoding.UTF8.GetString(x)).ToArray();
            Assert.AreEqual("1234", truncated[0]);
        }

        [Test]
        public void TestTruncateNoOverflow()
        {
            var targetSize = 1000 * 1000;
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, targetSize, new MessageOnlyFormatterForTest());

            var logBuilder = new StringBuilder();
            for (var i = 0; i < targetSize; i++) {
                logBuilder.Append("a");
            }
            var log = logBuilder.ToString();

            var truncated = logFormatter.TruncateIfNeeded(log).Select(x => Encoding.UTF8.GetString(x)).ToArray();
            var final = logFormatter.ToDDPayload(truncated[0]);

            // We account for 2x `...TRUNCATED...` in every chunk. 
            // Since the first chunk is only contains a trailing `...TRUNCATED...` - we only have to account for
            // a single instance in the final bytes. 
            var underflow = "...TRUNCATED...".Count();
            Assert.AreEqual(targetSize, final.Count() + underflow);

            // Check that we didn't lose any bytes in the source log
            Assert.AreEqual(targetSize, truncated[0].Count() + truncated[1].Count() - (2 * "...TRUNCATED...".Count()));
        }

        [Test]
        public void TestCustomJsonValueFormatter()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var logFormatter = new DatadogLogRenderer("TEST", "TEST", "localhost", new[] { "the", "coolest", "test" }, 256 * 1000, new DatadogJsonFormatter(jsonValueFormatter: new JsonValueFormatter(typeTagName: null)));
            var noop = new NoopClient(apiKey, logFormatter);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                var valueHolder = new ValueHolder{ Value = "some_value" };
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
                    { "creator", "ACME" },
                    { "value_holder", valueHolder },
                }, elapsedMs));
            }

            // Scrub the timestamp since this changes
            var scrubbed = Regex.Replace(noop.SentPayloads[0], "\\\"timestamp.*?,", "");
            Assert.AreEqual(@"{""ddsource"":""TEST"",""service"":""TEST"",""host"":""localhost"",""ddtags"":""the,coolest,test"",""message"":""{\\""message\"":\""Processed [(\\\""positions\\\"": [{ Latitude: 0, Longitude: 255 }, { Latitude: -32768, Longitude: 32767 }, { Latitude: -2147483648, Longitude: 2147483647 }, { Latitude: -9223372036854775808, Longitude: 9223372036854775807 }]), (\\\""creator\\\"": \\\""ACME\\\""), (\\\""value_holder\\\"": ValueHolder { Value: \\\""some_value\\\"" })] in 034 ms.\"",\""MessageTemplate\"":\""Processed {@Positions} in {Elapsed:000} ms.\"",\""level\"":\""Information\"",\""Properties\"":{\""Positions\"":{\""positions\"":[{\""Latitude\"":0,\""Longitude\"":255},{\""Latitude\"":-32768,\""Longitude\"":32767},{\""Latitude\"":-2147483648,\""Longitude\"":2147483647},{\""Latitude\"":-9223372036854775808,\""Longitude\"":9223372036854775807}],\""creator\"":\""ACME\"",\""value_holder\"":{\""Value\"":\""some_value\""}},\""Elapsed\"":34},\""Renderings\"":[\""034\""]}""}", scrubbed);
        }
    }
}