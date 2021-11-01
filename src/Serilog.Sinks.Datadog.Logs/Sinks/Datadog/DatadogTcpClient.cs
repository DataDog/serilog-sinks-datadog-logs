// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Serilog.Debugging;
using System.Collections.Generic;
using Serilog.Events;
using System.Net.NetworkInformation;
using System.Net;

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// TCP Client that forwards log events to Datadog.
    /// </summary>
    public class DatadogTcpClient : IDatadogClient
    {
        private readonly DatadogConfiguration _config;
        private readonly LogFormatter _formatter;
        private readonly string _apiKey;
        private readonly bool _detectTCPDisconnection;
        private TcpClient _client;
        private Stream _stream;
        private ConnectionMatcher _connectionMatcher;

        /// <summary>
        /// API Key / message-content delimiter.
        /// </summary>
        private const string WhiteSpace = " ";

        /// <summary>
        /// Message delimiter.
        /// </summary>
        private const string MessageDelimiter = "\n";

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

        public DatadogTcpClient(DatadogConfiguration config, LogFormatter formatter, string apiKey, bool detectTCPDisconnection)
        {
            _config = config;
            _formatter = formatter;
            _apiKey = apiKey;
            _detectTCPDisconnection = detectTCPDisconnection;
        }

        /// <summary>
        /// Initialize a connection to Datadog logs-backend.
        /// </summary>
        private async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_config.Url, _config.Port);
            _connectionMatcher = ConnectionMatcher.TryCreate(_client.Client.LocalEndPoint, _client.Client.RemoteEndPoint);

            Stream rawStream = _client.GetStream();
            if (_config.UseSSL)
            {
                SslStream secureStream = new SslStream(rawStream);
                await secureStream.AuthenticateAsClientAsync(_config.Url);
                _stream = secureStream;
            }
            else
            {
                _stream = rawStream;
            }
        }

        public async Task WriteAsync(IEnumerable<LogEvent> events)
        {
            var payloadBuilder = new StringBuilder();
            foreach (var logEvent in events)
            {
                payloadBuilder.Append(_apiKey + WhiteSpace);
                payloadBuilder.Append(_formatter.FormatMessage(logEvent));
                payloadBuilder.Append(MessageDelimiter);
            }
            string payload = payloadBuilder.ToString();

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                int backoff = (int)Math.Min(Math.Pow(retry, 2), MaxBackoff);
                if (retry > 0)
                {
                    await Task.Delay(backoff * 1000);
                }

                if (IsConnectionClosed())
                {
                    try
                    {
                        await ConnectAsync();
                    }
                    catch (Exception e)
                    {
                        SelfLog.WriteLine("Could not connect to Datadog: {0}", e);
                        continue;
                    }
                }

                try
                {
                    byte[] data = UTF8.GetBytes(payload);
                    _stream.Write(data, 0, data.Length);
                    return;
                }
                catch (Exception e)
                {
                    CloseConnection();
                    SelfLog.WriteLine("Could not send data to Datadog: {0}", e);
                }
            }
            SelfLog.WriteLine("Could not send payload to Datadog: {0}", payload);
        }

        private void CloseConnection()
        {
#if NETSTANDARD1_3
            _client.Dispose();
            _stream.Dispose();
#else
            _client.Close();
            _stream.Close();
#endif
            _stream = null;
            _client = null;
            _connectionMatcher = null;
        }

        private bool IsConnectionClosed()
        {
            if (_client == null || _stream == null)
            {
                return true;
            }
            if (_detectTCPDisconnection)
            {
                // `IPGlobalProperties` does not exist in NetStandard 1.3, keep the same behavior as before.
#if !NETSTANDARD1_3
                TcpConnectionInformation[] connections = null;
                try
                {
                    connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                }
                catch (NotImplementedException)
                {
                    // Happen when using Mono on MacOs. Keep the same behavior as before.
                    return false;
                }

                if (_connectionMatcher != null)
                {
                    var currentConnection = connections.FirstOrDefault(
                        c => _connectionMatcher.IsSameConnection(c.LocalEndPoint, c.RemoteEndPoint));

                    if (currentConnection == null || currentConnection.State != TcpState.Established)
                    {
                        SelfLog.WriteLine("TCP connection not established. Current state: {0}", currentConnection?.State);

                        return true;
                    }
                }
#endif
            }
            return false;
        }

        /// <summary>
        /// Close the client.
        /// </summary>
        public void Close()
        {
            if (!IsConnectionClosed())
            {
                try
                {
                    _stream.Flush();
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine("Could not flush the remaining data: {0}", e);
                }
                CloseConnection();
            }
        }
    }
}
