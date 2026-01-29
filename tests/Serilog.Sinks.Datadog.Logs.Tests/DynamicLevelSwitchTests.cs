using System;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class DynamicLevelSwitchTests
    {
        [Test]
        public void SinkHonorsLoggingLevelSwitchAtRuntime()
        {
            const string apiKey = "NOT_AN_API_KEY";
            var renderer = new DatadogLogRenderer("TEST", "TEST", "host", new[] { "tag1" }, 256 * 1000, new DatadogJsonFormatter());
            var noop = new NoopClient(apiKey, renderer);
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);

            using (var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.DatadogLogs(apiKey, client: noop, restrictedToMinimumLevel: LevelAlias.Minimum, levelSwitch: levelSwitch)
                .CreateLogger())
            {
                log.Information("info");
                // Below initial Warning threshold -> not sent
            }
            Assert.AreEqual(0, noop.SentPayloads.Count);

            // Turn level down and write again
            levelSwitch.MinimumLevel = LogEventLevel.Information;
            using (var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.DatadogLogs(apiKey, client: noop, restrictedToMinimumLevel: LevelAlias.Minimum, levelSwitch: levelSwitch)
                .CreateLogger())
            {
                log.Information("info2");
            }
            Assert.AreEqual(1, noop.SentPayloads.Count);
        }
    }
}

