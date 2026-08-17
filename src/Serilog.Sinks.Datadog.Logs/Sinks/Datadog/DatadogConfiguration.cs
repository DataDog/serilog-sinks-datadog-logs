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
        /// The default Datadog logs-backend URL (HTTP intake on the default site).
        /// Retained so the historical default for callers using <see cref="DatadogConfiguration()"/>
        /// without specifying a URL is preserved (master behavior).
        /// </summary>
        public const string DDUrl = "https://http-intake.logs.datadoghq.com";

        /// <summary>
        /// The default Datadog site.
        /// </summary>
        public const string DefaultSite = "datadoghq.com";

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
        /// HTTP intake URL when an explicit Url is not provided.
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// URL of the server to send log events to. Defaults to <see cref="DDUrl"/> for backward
        /// compatibility. If <see cref="Site"/> is set, it takes precedence over the default URL.
        /// To use a fully custom URL (e.g. a proxy), set this and leave <see cref="Site"/> unset.
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

        public DatadogConfiguration() : this(DDUrl, DDPort, true, false) {
        }

        public DatadogConfiguration(string url = DDUrl, int port = DDPort, bool useSSL = true, bool useTCP = false, int maxRetries = 10, bool resolveHostIfMissing = false, string site = null)
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
        /// Resolve the URL to use for the HTTP intake. A custom <see cref="Url"/> (anything other
        /// than the historical <see cref="DDUrl"/> default) always wins. Otherwise, if
        /// <see cref="Site"/> is set, derive <c>https://http-intake.logs.{site}</c>. Falls back to
        /// <see cref="Url"/> (which is <see cref="DDUrl"/> by default) when neither is set.
        /// </summary>
        internal string EffectiveHttpUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Url) && Url != DDUrl) return Url;
                if (!string.IsNullOrWhiteSpace(Site)) return $"https://http-intake.logs.{Site}";
                return Url;
            }
        }

        public override string ToString() => $"{{ Url: {Url}, Site: {Site}, Port: {Port}, UseSSL: {UseSSL}, UseTCP: {UseTCP} }}";
    }
}
