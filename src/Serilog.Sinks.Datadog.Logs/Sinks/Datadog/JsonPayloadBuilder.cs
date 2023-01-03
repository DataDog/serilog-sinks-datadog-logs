// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.Text;
using Serilog.Events;
using System.Collections.Generic;

namespace Serilog.Sinks.Datadog.Logs
{
    internal class JsonPayloadBuilder
    {
        private int _size = 2; // To account for "[" and "]"
        private string _delimiter = "";
        private StringBuilder _buffer = new StringBuilder();
        public List<LogEvent> LogEvents = new List<LogEvent>();

        public JsonPayloadBuilder()
        {
            _buffer.Append("[");
        }

        public int Size()
        {
            return _size;
        }

        public int Count()
        {
            return LogEvents.Count;
        }

        public void Add(string payload, LogEvent logEvent)
        {
            _buffer.Append(_delimiter);
            _buffer.Append(payload);
            LogEvents.Add(logEvent);

            _size += Encoding.UTF8.GetByteCount(payload) + _delimiter.Length;
            _delimiter = ",";
        }

        public string Build()
        {
            _buffer.Append("]");
            return _buffer.ToString();
        }
    }
}