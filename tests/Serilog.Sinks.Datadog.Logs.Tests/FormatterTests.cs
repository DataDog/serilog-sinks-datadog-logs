using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class FormatterTests
    {
        [Test]
        public void CanFormat()
        {
#if NET5_0_OR_GREATER
            var ver = Environment.Version.ToString();
#else
            var ver = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
#endif
            const string apiKey = "NOT_AN_API_KEY";
            var config = new DatadogConfiguration();
            var logFormatter = new LogFormatter(ver, "TEST", "localhost", new[] { "the", "coolest", "test" });
            var noop = new NoopClient(apiKey, logFormatter);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, configuration: config, client: noop).CreateLogger())
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
        }

        public class ExtendedFormatter : LogFormatter
        {
            public ExtendedFormatter(string source, string service, string host, string[] tags)
                : base(source, service, host, tags)
            {
            }

            protected override void TransformPayload(Dictionary<string, object> payload)
            {
                payload["ddtags"] = new[] { "newtags" };
            }
        }

        [Test]
        public void CanExtendFormatter()
        {
#if NET5_0_OR_GREATER
            var ver = Environment.Version.ToString();
#else
            var ver = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
#endif
            const string apiKey = "NOT_AN_API_KEY";
            var config = new DatadogConfiguration();
            var logFormatter = new ExtendedFormatter(ver, "TEST", "localhost", new[] { "the", "coolest", "test" });
            var noop = new NoopClient(apiKey, logFormatter);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, configuration: config, client: noop).CreateLogger())
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

            Assert.IsTrue(noop.LastLog.Contains("\"ddtags\":[\"newtags\"]"));
        }
    }
}