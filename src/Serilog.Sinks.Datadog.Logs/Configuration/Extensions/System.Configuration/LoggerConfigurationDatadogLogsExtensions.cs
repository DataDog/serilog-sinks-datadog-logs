// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;

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
        /// <param name="logLevel">The minimum log level for the sink.</param>
        /// <param name="batchSizeLimit">The maximum number of events to emit in a single batch.</param>
        /// <param name="batchPeriod">The time to wait before emitting a new event batch.</param>
        /// <param name="queueLimit">
        /// Maximum number of events to hold in the sink's internal queue, or <c>null</c>
        /// for an unbounded queue. The default is <c>10000</c>
        /// </param>
        /// <param name="exceptionHandler">This function is called when an exception occurs when using 
        /// DatadogConfiguration.UseTCP=false (the default configuration)</param>
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
            LogEventLevel logLevel = LevelAlias.Minimum,
            int? batchSizeLimit = null,
            TimeSpan? batchPeriod = null,
            int? queueLimit = null,
            Action<Exception> exceptionHandler = null)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            configuration = (configuration != null) ? configuration : new DatadogConfiguration();
            var sink = DatadogSink.Create(apiKey, source, service, host, tags, configuration, batchSizeLimit, batchPeriod, queueLimit, exceptionHandler);

            return loggerConfiguration.Sink(sink, logLevel);
        }
    }
}
