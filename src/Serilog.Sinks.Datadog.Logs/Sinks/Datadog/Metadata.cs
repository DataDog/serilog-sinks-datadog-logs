// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Events;
using System.Collections.Generic;

namespace Serilog.Sinks.Datadog.Logs
{
    public class MetadataEnricher
    {
        private const string CSHARP = "csharp";
        private readonly LogEventProperty _ddProperties;

        public MetadataEnricher(string source, string service, string host, string[] tags)
        {
            var props = new List<LogEventProperty> {
                new LogEventProperty("ddsource", new ScalarValue(source ?? CSHARP)),
            };
            if (service != null) { props.Add(new LogEventProperty("service", new ScalarValue(service))); }
            if (host != null) { props.Add(new LogEventProperty("host", new ScalarValue(host))); }
            if (tags != null) { props.Add(new LogEventProperty("ddtags", new ScalarValue(string.Join(",", tags)))); }
            _ddProperties = new LogEventProperty("ddproperties", new StructureValue(props));
        }

        public void Enrich(LogEvent logEvent) 
        {
            logEvent.AddOrUpdateProperty(_ddProperties);
        }
    }
}
