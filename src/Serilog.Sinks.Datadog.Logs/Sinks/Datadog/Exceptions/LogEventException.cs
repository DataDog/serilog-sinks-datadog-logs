// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2020 Datadog, Inc.

using System;
using Serilog.Events;
using System.Collections.Generic;

namespace Serilog.Sinks.Datadog.Logs
{
    public class LogEventException : Exception
    {
        public LogEventException(string message, IEnumerable<LogEvent> logEvents)
            : base(message)
        {
            LogEvents = logEvents;
        }

        public IEnumerable<LogEvent> LogEvents { get; }
    }
}