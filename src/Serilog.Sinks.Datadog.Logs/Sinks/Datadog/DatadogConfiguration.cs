﻿// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

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
        public const string DDUrl = "intake.logs.datadoghq.com";

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
        public string Url { get; private set; }

        /// <summary>
        /// Port of the server to send log events to.
        /// </summary>
        public int Port { get; private set;  }

        /// <summary>
        /// Use SSL or plain text.
        /// </summary>
        public bool UseSSL { get; private set; }

        public DatadogConfiguration(string url = DDUrl, int port = DDPort, bool useSSL =  true)
        {
            Url = url;
            Port = port;
            UseSSL = useSSL;
        }
    }
}