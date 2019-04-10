// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// Configuration used by the DatadogClient to forward log events to a remote backend.
    /// </summary>
    public class DatadogConfiguration
    {
        /// <summary>
        /// The Datadog logs-backend URL.
        /// </summary>
        public const string DDUrl = "https://http-intake.logs.datadoghq.com";

        /// <summary>
        /// The Datadog logs-backend TCP SSL port.
        /// </summary>
        public const int DDPort = 10516;

        /// <summary>
        /// The Datadog logs-backend TCP unsecure port.
        /// </summary>
        public const int DDPortNoSSL = 10514;

        /// <summary>
        /// URL of the server to send log events to.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Port of the server to send log events to.
        /// </summary>
        public int Port { get; set;  }

        /// <summary>
        /// Use SSL or plain text.
        /// </summary>
        public bool UseSSL { get; set; }

        /// <summary>
        /// Use TCP or HTTP.
        /// </summary>
        public bool UseTCP { get; set; }

        public DatadogConfiguration() : this(DDUrl, DDPort, true, false) {
        }

        public DatadogConfiguration(string url = DDUrl, int port = DDPort, bool useSSL = true, bool useTCP = false)
        {
            Url = url;
            Port = port;
            UseSSL = useSSL;
            UseTCP = useTCP;
        }

        public override string ToString()
        {
            return string.Format("{{ Url: {0}, Port: {1}, UseSSL: {2}, UseTCP: {3} }}", Url, Port, UseSSL, UseTCP);
        }
    }
}
