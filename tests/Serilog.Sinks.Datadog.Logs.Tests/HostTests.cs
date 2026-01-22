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

        [Test]
        public void EventHostWhitespaceDoesNotOverride()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var configuredHost = "configured-host";
            var renderer = new DatadogLogRenderer("TEST", "TEST", configuredHost, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", "   ").Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + configuredHost + "\"", payload);
        }

        [Test]
        public void EventHostNonStringDoesNotOverride()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var configuredHost = "configured-host";
            var renderer = new DatadogLogRenderer("TEST", "TEST", configuredHost, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", 12345).Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + configuredHost + "\"", payload);
        }

        [Test]
        public void HostOverrideAppliesOnlyToThatEvent()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var configuredHost = "configured-host";
            var overrideHost = "override-host";
            var renderer = new DatadogLogRenderer("TEST", "TEST", configuredHost, new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", overrideHost).Information("first");
                log.Information("second");
            }

            Assert.AreEqual(2, noop.SentPayloads.Count);
            var first = noop.SentPayloads[0];
            var second = noop.SentPayloads[1];

            StringAssert.Contains("\"host\":\"" + overrideHost + "\"", first);
            StringAssert.DoesNotContain("\"host\":\"" + configuredHost + "\"", first);

            StringAssert.Contains("\"host\":\"" + configuredHost + "\"", second);
            StringAssert.DoesNotContain("\"host\":\"" + overrideHost + "\"", second);
        }

        [Test]
        public void ServiceAndTagsPreservedWhenOverridingHost()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var configuredHost = "configured-host";
            var overrideHost = "override-host";
            var renderer = new DatadogLogRenderer("TEST-SRC", "TEST-SVC", configuredHost, new[] { "a:1", "b:2" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", overrideHost).Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"" + overrideHost + "\"", payload);
            StringAssert.Contains("\"service\":\"TEST-SVC\"", payload);
            StringAssert.Contains("\"ddtags\":\"a:1,b:2\"", payload);
            StringAssert.Contains("\"ddsource\":\"TEST-SRC\"", payload);
        }
    }
}

