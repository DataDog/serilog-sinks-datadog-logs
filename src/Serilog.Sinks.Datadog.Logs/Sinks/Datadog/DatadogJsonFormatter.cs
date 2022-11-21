// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Templates;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogJsonFormatter: ExpressionTemplate
    {
        public DatadogJsonFormatter() : base(@"{ {
            Timestamp: @t,
            level: @l,
            MessageTemplate: @mt,
            message: @m, 
            Properties: {..@p, ddproperties: undefined()},
            Renderings: @r, 
            Exception: @x,
            EventId: @i,
            ddsource: @p['ddproperties']['ddsource'], 
            service: @p['ddproperties']['service'], 
            host: @p['ddproperties']['host'], 
            ddtags: @p['ddproperties']['ddtags']} 
        }") {}
    }
}
