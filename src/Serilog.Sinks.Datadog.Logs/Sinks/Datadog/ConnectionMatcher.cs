using System.Net;

namespace Serilog.Sinks.Datadog.Logs
{
    internal class ConnectionMatcher
    {
        readonly private IPEndPoint _localIPv4;
        readonly private IPEndPoint _localIPv6;
        readonly private IPEndPoint _remoteIPv4;
        readonly private IPEndPoint _remoteIPv6;

        public static ConnectionMatcher TryCreate(EndPoint local, EndPoint remote)
        {
            if (local is IPEndPoint localIp && remote is IPEndPoint remoteIp)
            {
                return new ConnectionMatcher(
                    new IPEndPoint(localIp.Address.MapToIPv4(), localIp.Port),
                    new IPEndPoint(localIp.Address.MapToIPv6(), localIp.Port),
                    new IPEndPoint(remoteIp.Address.MapToIPv4(), remoteIp.Port),
                    new IPEndPoint(remoteIp.Address.MapToIPv6(), remoteIp.Port));
            }
            return null;
        }

        public bool IsSameConnection(EndPoint local, EndPoint remote)
        {
            return (_localIPv4.Equals(local) || _localIPv6.Equals(local))
            && (_remoteIPv4.Equals(remote) || _remoteIPv6.Equals(remote));
        }

        private ConnectionMatcher(
            IPEndPoint localIPv4,
            IPEndPoint localIPv6,
            IPEndPoint remoteIPv4,
            IPEndPoint remoteIPv6)
        {
            _localIPv4 = localIPv4;
            _localIPv6 = localIPv6;
            _remoteIPv4 = remoteIPv4;
            _remoteIPv6 = remoteIPv6;
        }
    }
}
