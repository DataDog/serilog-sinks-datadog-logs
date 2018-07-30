﻿// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

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
        /// <param name="configuration">The Datadog logs client configuration.</param>
        /// <returns>Logger configuration</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration DatadogLogs(
            this LoggerSinkConfiguration loggerConfiguration,
            string apiKey,
            DatadogConfiguration configuration = null,
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

            configuration = (configuration != null) ? configuration : new DatadogConfiguration();
            return loggerConfiguration.Sink(new DatadogSink(apiKey, configuration), logLevel);
        }
    }
}
