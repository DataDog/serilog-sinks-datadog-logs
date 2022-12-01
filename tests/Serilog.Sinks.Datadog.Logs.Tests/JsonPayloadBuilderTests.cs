using System;
using System.Collections.Generic;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    class JsonPayloadBuilderTests
    {
        private LogEvent newLogEvent(string message)
        {
            return new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate(message, new List<MessageTemplateToken>()), new List<LogEventProperty>());
        }

        [Test]
        public void TestPayloadBuilder()
        {
            var builder = new JsonPayloadBuilder();

            Assert.AreEqual(2, builder.Size());
            Assert.AreEqual("[]", builder.Build());

            builder = new JsonPayloadBuilder();
            builder.Add("foo", newLogEvent("foo"));

            Assert.AreEqual(5, builder.Size());
            Assert.AreEqual(1, builder.Count());
            Assert.AreEqual("[foo]", builder.Build());

            builder = new JsonPayloadBuilder();
            builder.Add("foo", newLogEvent("foo"));
            builder.Add("bar", newLogEvent("bar"));

            Assert.AreEqual(9, builder.Size());
            Assert.AreEqual(2, builder.Count());
            Assert.AreEqual("[foo,bar]", builder.Build());

            builder = new JsonPayloadBuilder();
            builder.Add("foo", newLogEvent("foo"));
            builder.Add("bar", newLogEvent("bar"));
            builder.Add("test-string", newLogEvent("bar"));

            Assert.AreEqual(21, builder.Size());
            Assert.AreEqual(3, builder.Count());
            Assert.AreEqual("[foo,bar,test-string]", builder.Build());
        }
    }
}
