// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Microsoft.Extensions.Configuration;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;
using System;
using System.Linq;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.Datadog() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationDatadogExtensions
    {
        /// <summary>
        /// Adds a sink that sends log events to Datadog.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="apiKey">Your Datadog API key.</param>
        /// <param name="source">The integration name.</param>
        /// <param name="service">The service name.</param>
        /// <param name="host">The host name.</param>
        /// <param name="tags">Custom tags.</param>
        /// <param name="configuration">The Datadog logs client configuration.</param>
        /// <param name="configurationSection">A config section defining the datadog configuration.</param>
        /// <param name="sinkConfigurationSection">A config section defining the datadog sink configuration.</param>
        /// <param name="logLevel">The minimum log level for the sink.</param>
        /// <param name="batchSizeLimit">The maximum number of events to emit in a single batch.</param>
        /// <param name="batchPeriod">The time to wait before emitting a new event batch.</param>
        /// <param name="queueLimit">
        /// Maximum number of events to hold in the sink's internal queue, or <c>null</c>
        /// for an unbounded queue. The default is <c>10000</c>
        /// </param>
        /// <param name="exceptionHandler">This function is called when an exception occurs when using
        /// DatadogConfiguration.UseTCP=false (the default configuration)</param>
        /// <param name="detectTCPDisconnection">Detect when the TCP connection is lost and recreate a new connection.</param>
        /// <returns>Logger configuration</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration DatadogLogs(
            this LoggerSinkConfiguration loggerConfiguration,
            string apiKey,
            string source = null,
            string service = null,
            string host = null,
            string[] tags = null,
            DatadogConfiguration configuration = null,
            IConfigurationSection configurationSection = null,
            IConfigurationSection sinkConfigurationSection = null,
            LogEventLevel logLevel = LevelAlias.Minimum,
            int? batchSizeLimit = null,
            TimeSpan? batchPeriod = null,
            int? queueLimit = null,
            Action<Exception> exceptionHandler = null,
            bool detectTCPDisconnection = false, IDatadogClient client = null)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            ConfigureSink(sinkConfigurationSection, ref source, ref service, ref host, ref tags, ref logLevel, ref batchSizeLimit, ref batchPeriod, ref queueLimit);
            var config = ApplyMicrosoftExtensionsConfiguration.ConfigureDatadogConfiguration(configuration, configurationSection);
            var sink = DatadogSink.Create(apiKey, source, service, host, tags, config, batchSizeLimit, batchPeriod, queueLimit, exceptionHandler, detectTCPDisconnection, client);

            return loggerConfiguration.Sink(sink, logLevel);
        }


        /// <summary>
        /// Configure the sink from the provided IConfigurationSection if the sink options have not already been set
        /// </summary>
        private static void ConfigureSink(IConfigurationSection sinkConfigurationSection, ref string source, ref string service, ref string host, ref string[] tags, ref LogEventLevel logLevel, ref int? batchSizeLimit, ref TimeSpan? batchPeriod, ref int? queueLimit)
        {
            if (sinkConfigurationSection == null)
            {
                return;
            }

            TrySetProperty(sinkConfigurationSection["source"], ref source);
            TrySetProperty(sinkConfigurationSection["service"], ref service);
            TrySetProperty(sinkConfigurationSection["host"], ref host);

            if (sinkConfigurationSection.GetSection("tags") != null)
            {
                 tags = sinkConfigurationSection.GetSection("tags").GetChildren().Select(m => m.Value).Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(sinkConfigurationSection["logLevel"]))
            {
                Enum.TryParse(sinkConfigurationSection["logLevel"], ignoreCase: true, out logLevel);
            }

            TrySetProperty(sinkConfigurationSection["batchSizeLimit"], ref batchSizeLimit);
            TrySetProperty(sinkConfigurationSection["queueLimit"], ref queueLimit);

            if (!batchPeriod.HasValue && !string.IsNullOrWhiteSpace(sinkConfigurationSection["batchPeriod"]))
            {
                if (TimeSpan.TryParse(sinkConfigurationSection["batchPeriod"], out var batchPeriodOverride))
                    batchPeriod = batchPeriodOverride;
            }
        }

        private static void TrySetProperty(string source, ref string target)
        {
            if (string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            target = source;
        }

        private static void TrySetProperty(string source, ref int? target)
        {
            if (target.HasValue || string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            if (int.TryParse(source, out var value))
                target = value;
        }
    }
}
