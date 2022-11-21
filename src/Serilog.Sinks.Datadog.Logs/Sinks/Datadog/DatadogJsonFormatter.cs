// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Templates;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogJsonFormatter: ExpressionTemplate
    {
        public DatadogJsonFormatter() : base("{ {timestamp: @t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, ..@p} }\n") {}
    }
}
