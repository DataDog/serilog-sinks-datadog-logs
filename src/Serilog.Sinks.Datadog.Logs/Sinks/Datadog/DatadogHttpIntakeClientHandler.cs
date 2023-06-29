using System.Net;
using System.Net.Http;

namespace Serilog.Sinks.Datadog.Logs
{
    internal class DatadogHttpIntakeClientHandler : HttpClientHandler
    {
        public DatadogHttpIntakeClientHandler(string host, int port)
        {
            if (!string.IsNullOrEmpty(host))
            {
                if (port != 0)
                {
                    Proxy = new WebProxy(host, port);
                }
                else
                {
                    Proxy = new WebProxy(host);
                }
            }
        }
    }
}
