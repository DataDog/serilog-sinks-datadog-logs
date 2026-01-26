using System;
using NUnit.Framework;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class EnvFallbackTests
    {
        [SetUp]
        public void ClearDdEnv()
        {
            Environment.SetEnvironmentVariable("DD_SERVICE", null);
            Environment.SetEnvironmentVariable("DD_SOURCE", null);
            Environment.SetEnvironmentVariable("DD_HOST", null);
            Environment.SetEnvironmentVariable("DD_TAGS", null);
            Environment.SetEnvironmentVariable("DD_ENV", null);
            Environment.SetEnvironmentVariable("DD_VERSION", null);
        }

        [Test]
        public void ExplicitArgsOverrideEnv_ForSourceServiceHost()
        {
            Environment.SetEnvironmentVariable("DD_SERVICE", "svc-from-env");
            Environment.SetEnvironmentVariable("DD_SOURCE", "src-from-env");
            Environment.SetEnvironmentVariable("DD_HOST", "host-from-env");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC-ARG", "SVC-ARG", "HOST-ARG", null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"ddsource\":\"SRC-ARG\"", payload);
            StringAssert.Contains("\"service\":\"SVC-ARG\"", payload);
            StringAssert.Contains("\"host\":\"HOST-ARG\"", payload);
            StringAssert.DoesNotContain("\"ddsource\":\"src-from-env\"", payload);
            StringAssert.DoesNotContain("\"service\":\"svc-from-env\"", payload);
            StringAssert.DoesNotContain("\"host\":\"host-from-env\"", payload);
        }

        [Test]
        public void UsesDdServiceWhenServiceUnset()
        {
            Environment.SetEnvironmentVariable("DD_SERVICE", "svc-from-env");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer(null, null, "localhost", null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"service\":\"svc-from-env\"", payload);
        }

        [Test]
        public void UsesDdSourceWhenSourceUnset()
        {
            Environment.SetEnvironmentVariable("DD_SOURCE", "csharp-env");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer(null, "svc", "localhost", null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"ddsource\":\"csharp-env\"", payload);
        }

        [Test]
        public void UsesDdHostWhenHostUnset()
        {
            Environment.SetEnvironmentVariable("DD_HOST", "env-host");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC", "SVC", null, null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"host\":\"env-host\"", payload);
        }

        [Test]
        public void MergesDdTagsEnvAndVersionIntoDdtags()
        {
            Environment.SetEnvironmentVariable("DD_TAGS", "a:1,b:2");
            Environment.SetEnvironmentVariable("DD_ENV", "prod");
            Environment.SetEnvironmentVariable("DD_VERSION", "1.2.3");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC", "SVC", "HOST", new[] { "x:9" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }

            var payload = noop.SentPayloads[0];
            StringAssert.Contains("\"ddtags\":\"x:9,a:1,b:2,env:prod,version:1.2.3\"", payload);
        }

        [Test]
        public void TagsParsingSupportsSpacesAndCommas()
        {
            Environment.SetEnvironmentVariable("DD_TAGS", "a:1 b:2");
            const string apiKey = "NOT_AN_API_KEY";
            var renderer1 = new DatadogLogRenderer("SRC", "SVC", "HOST", null, 256 * 1000, new DatadogJsonFormatter());
            var noop1 = new NoopClient(apiKey, renderer1);

            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop1).CreateLogger())
            {
                log.Information("one");
            }
            var p1 = noop1.SentPayloads[0];
            StringAssert.Contains("\"ddtags\":\"a:1,b:2\"", p1);

            Environment.SetEnvironmentVariable("DD_TAGS", "a:1,b:2");
            var renderer2 = new DatadogLogRenderer("SRC", "SVC", "HOST", null, 256 * 1000, new DatadogJsonFormatter());
            var noop2 = new NoopClient(apiKey, renderer2);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop2).CreateLogger())
            {
                log.Information("two");
            }
            var p2 = noop2.SentPayloads[0];
            StringAssert.Contains("\"ddtags\":\"a:1,b:2\"", p2);
        }

        [Test]
        public void ExplicitTagsMergedWithEnvAndDeduped()
        {
            Environment.SetEnvironmentVariable("DD_TAGS", "a:1 b:2");
            Environment.SetEnvironmentVariable("DD_ENV", "prod");
            Environment.SetEnvironmentVariable("DD_VERSION", "1.2.3");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC", "SVC", "HOST", new[] { "a:1", "x:9" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }
            var payload = noop.SentPayloads[0];
            // a:1 should appear only once, and all tags present
            StringAssert.Contains("\"ddtags\":\"a:1,x:9,b:2,env:prod,version:1.2.3\"", payload);
        }

        [Test]
        public void WhitespaceEnvValuesAreIgnored_NoDdtagsWhenEmpty()
        {
            Environment.SetEnvironmentVariable("DD_TAGS", "   ");
            Environment.SetEnvironmentVariable("DD_ENV", "   ");
            Environment.SetEnvironmentVariable("DD_VERSION", "   ");

            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC", "SVC", "HOST", null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.Information("hello");
            }
            var payload = noop.SentPayloads[0];
            StringAssert.DoesNotContain("\"ddtags\":", payload);
        }

        [Test]
        public void EventHostOverridesEnvHost_ForThatEventOnly()
        {
            Environment.SetEnvironmentVariable("DD_HOST", "env-host");
            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("SRC", "SVC", null, null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);
            using (var log = new LoggerConfiguration().WriteTo.DatadogLogs(apiKey, client: noop).CreateLogger())
            {
                log.ForContext("host", "event-host").Information("one");
                log.Information("two");
            }

            Assert.AreEqual(2, noop.SentPayloads.Count);
            var first = noop.SentPayloads[0];
            var second = noop.SentPayloads[1];
            StringAssert.Contains("\"host\":\"event-host\"", first);
            StringAssert.Contains("\"host\":\"env-host\"", second);
        }
    }
}

