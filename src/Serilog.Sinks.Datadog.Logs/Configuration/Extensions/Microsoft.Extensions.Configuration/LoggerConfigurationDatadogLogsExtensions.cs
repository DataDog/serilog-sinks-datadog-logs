// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

using Microsoft.Extensions.Configuration;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;
using System;

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
        /// <param name="columnOptionsSection">A config section defining the datadog configuration.</param>
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
            LogEventLevel logLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            var config = ApplyMicrosoftExtensionsConfiguration.ConfigureDatadogConfiguration(configuration, configurationSection);

            return loggerConfiguration.Sink(new DatadogSink(apiKey, source, service, host, tags, config), logLevel);
        }
    }
}
