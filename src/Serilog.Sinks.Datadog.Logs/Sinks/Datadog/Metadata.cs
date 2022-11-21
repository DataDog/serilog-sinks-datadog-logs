// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using Serilog.Core;
using Serilog.Events;
using Serilog.Capturing;
using Serilog.Core.Enrichers;
using System.Collections.Generic;

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// </summary>
    public class MetadataEnricher
    {
        private const string CSHARP = "csharp";
        private readonly string _source;
        private readonly string _service;
        private readonly string _host;
        private readonly string _tags;

        public MetadataEnricher(string source, string service, string host, string[] tags)
        {
            _source = source ?? CSHARP;
            _service = service;
            _host = host;
            _tags = tags != null ? string.Join(",", tags) : null;
        }

        public void Enrich(LogEvent logEvent) 
        {
            var props = new List<LogEventProperty> {
                new LogEventProperty("ddsource", new ScalarValue(_source)),
            };
            if (_service != null) { props.Add(new LogEventProperty("service", new ScalarValue(_service))); }
            if (_host != null) { props.Add(new LogEventProperty("host", new ScalarValue(_host))); }
            if (_tags != null) { props.Add(new LogEventProperty("ddtags", new ScalarValue(_tags))); }
            logEvent.AddOrUpdateProperty(new LogEventProperty("ddproperties", new StructureValue(props)));
        }
    }
}
