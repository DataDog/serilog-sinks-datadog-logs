// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Events;

namespace Serilog.Sinks.Datadog.Logs
{
    public interface IDatadogClient : IDisposable
    {
        /// <summary>
        /// Send payload to Datadog logs-backend.
        /// </summary>
        /// <param name="events">Serilog events to send.</param>
        /// <param name="onException">A callback that is invoked whenever the client fails to write events.</param>
        Task WriteAsync(LogEvent[] events, Action<Exception> onException);

        /// <summary>
        /// Cleanup existing resources.
        /// </summary>
        void Close();
    }
}
