using System.Net;
using NUnit.Framework;

namespace Serilog.Sinks.Datadog.Logs.Tests
{
    [TestFixture]
    public class ConnectionMatcherTests
    {
        IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
        IPAddress remoteIPAddress = IPAddress.Parse("127.0.0.2");

        [Test]
        public void IsSameConnection()
        {
            EndPoint localEndPoint = new IPEndPoint(localIPAddress, 10);
            EndPoint remoteEndPoint = new IPEndPoint(remoteIPAddress, 20);

            var connectionMatcher = ConnectionMatcher.TryCreate(localEndPoint, remoteEndPoint);
            Assert.That(connectionMatcher.IsSameConnection(
                new IPEndPoint(localIPAddress.MapToIPv4(), 10),
                new IPEndPoint(remoteIPAddress.MapToIPv4(), 20)), Is.True);
            Assert.That(connectionMatcher.IsSameConnection(
                new IPEndPoint(localIPAddress.MapToIPv6(), 10),
                new IPEndPoint(remoteIPAddress.MapToIPv6(), 20)), Is.True);
            Assert.That(connectionMatcher.IsSameConnection(
                new IPEndPoint(remoteIPAddress.MapToIPv6(), 10),
                new IPEndPoint(remoteIPAddress.MapToIPv6(), 20)), Is.False);
        }
    }
}

