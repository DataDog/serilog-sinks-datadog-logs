// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.Net.Http;

namespace Serilog.Sinks.Datadog.Logs
{
    internal class DatadogHttpIntakeClient : HttpClient
    {
        public DatadogHttpIntakeClient(string apiKey)
        {
            DefaultRequestHeaders.Add("DD-API-KEY", apiKey);
            DefaultRequestHeaders.Add("DD-EVP-ORIGIN", "Serilog.Sinks.Datadog.Logs");
            DefaultRequestHeaders.Add("DD-EVP-ORIGIN-VERSION", Consts.Version);
        }
    }
}

