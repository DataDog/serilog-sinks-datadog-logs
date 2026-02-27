using System.Net;
using NUnit.Framework;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class ProxyConfigurationTests
    {
        [Test]
        public void Sink_CanBeCreated_WithProxyUrl()
        {
            var config = new DatadogConfiguration(proxyUrl: "http://proxy.example.com:8080");
            using (var log = new LoggerConfiguration()
                .WriteTo.DatadogLogs("NOT_AN_API_KEY", configuration: config)
                .CreateLogger())
            {
                Assert.IsNotNull(log);
            }
        }

        [Test]
        public void Sink_CanBeCreated_WithProxy()
        {
            var config = new DatadogConfiguration();
            config.Proxy = new WebProxy("http://proxy.example.com:8080");
            using (var log = new LoggerConfiguration()
                .WriteTo.DatadogLogs("NOT_AN_API_KEY", configuration: config)
                .CreateLogger())
            {
                Assert.IsNotNull(log);
            }
        }

        [Test]
        public void Sink_WithProxyUrl_AndCustomClient_LogsSuccessfully()
        {
            var config = new DatadogConfiguration(proxyUrl: "http://proxy.example.com:8080");
            var renderer = new DatadogLogRenderer("src", "svc", "host", null, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient("NOT_AN_API_KEY", renderer);

            using (var log = new LoggerConfiguration()
                .WriteTo.DatadogLogs("NOT_AN_API_KEY", configuration: config, client: noop)
                .CreateLogger())
            {
                log.Information("hello via proxy config");
            }

            Assert.AreEqual(1, noop.SentPayloads.Count);
            StringAssert.Contains("hello via proxy config", noop.SentPayloads[0]);
        }
    }
}
