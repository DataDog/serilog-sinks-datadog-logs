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
        /// When true, if the sink-level Host option is unset, resolve and include a default host value.
        /// </summary>
        public bool ResolveHostIfMissing { get; set; }

        /// <summary>
        /// The default Datadog site.
        /// </summary>
        public const string DefaultSite = "datadoghq.com";

        /// <summary>
        /// The Datadog logs-backend URL (US site).
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
        /// Datadog site (e.g. "datadoghq.com", "datadoghq.eu", "us3.datadoghq.com",
        /// "us5.datadoghq.com", "ap1.datadoghq.com", "ddog-gov.com"). Used to derive the
        /// HTTP/TCP intake hostname when an explicit Url is not provided.
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// URL of the server to send log events to. When unset (null), the effective URL is
        /// derived from <see cref="Site"/>. An explicit value here always wins over <see cref="Site"/>.
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

        /// <summary>
        /// Number of retries before the client gives up logging.
        /// </summary>
        public int MaxRetries { get; set; }

        public DatadogConfiguration() : this(null, DDPort, true, false) {
        }

        public DatadogConfiguration(string url = null, int port = DDPort, bool useSSL = true, bool useTCP = false, int maxRetries = 10, bool resolveHostIfMissing = false, string site = null)
        {
            Url = url;
            Port = port;
            UseSSL = useSSL;
            UseTCP = useTCP;
            MaxRetries = maxRetries;
            ResolveHostIfMissing = resolveHostIfMissing;
            Site = site;
        }

        /// <summary>
        /// Resolve the effective Datadog site, falling back to the default when none was set.
        /// </summary>
        internal string EffectiveSite => string.IsNullOrWhiteSpace(Site) ? DefaultSite : Site;

        /// <summary>
        /// Resolve the URL to use for the HTTP intake. An explicit <see cref="Url"/> wins;
        /// otherwise derives <c>https://http-intake.logs.{site}</c> from <see cref="EffectiveSite"/>.
        /// </summary>
        internal string EffectiveHttpUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Url)) return Url;
                return $"https://http-intake.logs.{EffectiveSite}";
            }
        }

        /// <summary>
        /// Resolve the hostname to use for the TCP intake. An explicit <see cref="Url"/> wins
        /// (treated as a hostname); otherwise derives <c>intake.logs.{site}</c> from
        /// <see cref="EffectiveSite"/>.
        /// </summary>
        internal string EffectiveTcpHost
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Url)) return Url;
                return $"intake.logs.{EffectiveSite}";
            }
        }

        public override string ToString() => $"{{ Url: {Url}, Site: {Site}, Port: {Port}, UseSSL: {UseSSL}, UseTCP: {UseTCP} }}";
    }
}
