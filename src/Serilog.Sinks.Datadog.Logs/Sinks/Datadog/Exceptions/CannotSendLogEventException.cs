// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2020 Datadog, Inc.

using Serilog.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Serilog.Sinks.Datadog.Logs
{
    public class CannotSendLogEventException : LogEventException
    {
        public CannotSendLogEventException(string payload, IReadOnlyCollection<LogEvent> logEvents)
            : base($"Could not send payload to Datadog: {payload}", logEvents)
        {
        }
    }
}