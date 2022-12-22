// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System.Net.Http;

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogHttpClient : DatadogHttpClientBase
    {
        private const string _version = "0.4.0";

        public DatadogHttpClient(DatadogConfiguration config, DatadogLogRenderer renderer, string apiKey)
            : base(config, CreateHttpClient(apiKey), $"{config.Url}/api/v2/logs", renderer)
        {
        }

        private static HttpClient CreateHttpClient(string apiKey)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("DD-API-KEY", apiKey);
            client.DefaultRequestHeaders.Add("DD-EVP-ORIGIN", "Serilog.Sinks.Datadog.Logs");
            client.DefaultRequestHeaders.Add("DD-EVP-ORIGIN-VERSION", _version);

            return client;
        }
    }
}
