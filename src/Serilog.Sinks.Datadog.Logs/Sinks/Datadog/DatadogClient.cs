// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2018 Datadog, Inc.

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Serilog.Debugging;

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// TCP Client that forwards log events to Datadog.
    /// </summary>
    public class DatadogClient
    {
        private readonly DatadogConfiguration _config;
        private TcpClient _client;
        private Stream _stream;

        /// <summary>
        /// Max number of retries when sending failed.
        /// </summary>
        private const int MaxRetries = 5;

        /// <summary>
        /// Max backoff used when sending failed.
        /// </summary>
        private const int MaxBackoff = 30;

        /// <summary>
        /// Shared UTF8 encoder.
        /// </summary>
        private static readonly UTF8Encoding UTF8 = new UTF8Encoding();

        public DatadogClient(DatadogConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Initialize a connection to Datadog logs-backend.
        /// </summary>
        private void connect()
        {
            _client = new TcpClient();
            _client.ConnectAsync(_config.Url, _config.Port).Wait();
            Stream rawStream = _client.GetStream();
            if (_config.UseSSL)
            {
                SslStream secureStream = new SslStream(rawStream);
                secureStream.AuthenticateAsClientAsync(_config.Url).Wait();
                _stream = secureStream;
            }
            else
            {
                _stream = rawStream;
            }
        }

        /// <summary>
        /// Send payload to Datadog logs-backend.
        /// </summary>
        /// <param name="payload">Payload to send.</param>
        public void write(string payload)
        {
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                int backoff = (int)Math.Min(Math.Pow(retry, 2), MaxBackoff);
                if (retry > 0)
                {                    
                    Thread.Sleep(backoff * 1000);
                }

                if (_client == null || _client.Connected == false)
                {
                    try
                    {
                        connect();
                    }
                    catch (Exception e)
                    {
                        SelfLog.WriteLine("Could not connect to Datadog: {0}", e);
                        continue;
                    }
                }

                try
                {
                    SelfLog.WriteLine("Sending payload to Datadog: {0}", payload);
                    byte[] data = UTF8.GetBytes(payload);
                    _stream.Write(data, 0, data.Length);
                    return;
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine("Could not send data to Datadog: {0}", e);
                }
            }
            SelfLog.WriteLine("Could not send payload to Datadog: {0}", payload);
        }

        /// <summary>
        /// Close the client.
        /// </summary>
        public void close()
        {
            if (_client != null)
            {
                try
                {
                    _stream.Flush();
#if NETSTANDARD1_3
                    _client.Dispose();
#else
                    _client.Close();
#endif
                    _client = null;
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine("Could not close the client: {0}", e);
                }
            }
        }
    }
}
