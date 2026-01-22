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
    }
}

