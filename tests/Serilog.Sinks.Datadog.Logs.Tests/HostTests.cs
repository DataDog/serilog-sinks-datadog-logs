using System;
using NUnit.Framework;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class HostTests
    {
        [Test]
        public void UnsetHostDefaultsToEnvironmentMachineName()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("TEST", "TEST", null, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + Environment.MachineName + "\"", payload);
        }

        [Test]
        public void ExplicitHostIsRespected()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var host = "explicit-host";
            var renderer = new DatadogLogRenderer("TEST", "TEST", host, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + host + "\"", payload);
        }

        [Test]
        public void EventHostPropertyOverridesConfiguredHost()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var configuredHost = "base-host";
            var eventHost = "event-host";
            var renderer = new DatadogLogRenderer("TEST", "TEST", configuredHost, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", eventHost).Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + eventHost + "\"", payload);
            StringAssert.DoesNotContain("\"host\":\"" + configuredHost + "\"", payload);
        }
    }
}

