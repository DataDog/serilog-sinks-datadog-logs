// Unless explicitly stated otherwise all files in this repository are licensed
// under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2019 Datadog, Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

[assembly: InternalsVisibleTo("Serilog.Sinks.Datadog.Logs.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100a188c93acb61ca68b3b11e5047e3602ffea902e7413310ce96cdd8e31992d36d9276cd36ce55b7870a39379fec698b458bebaa0dc8c72b5e438c7418d640c9bc46a21af3f08a48b68aa8ec23fe0d01bcdcfa5126c66e7586ae08dc1c21142b2c7d49cb09649a2fc9ba767fc88fee6347536a51d28ff398eaabb760494db90dd0")]

namespace Serilog.Sinks.Datadog.Logs
{
    public class DatadogSink : PeriodicBatchingSink
    {
        private readonly IDatadogClient _client;
        private readonly Action<Exception> _exceptionHandler;
        private readonly bool _recycleResources;

        /// <summary>
        /// The time to wait before emitting a new event batch.
        /// </summary>
        private static readonly TimeSpan DefaultBatchPeriod = TimeSpan.FromSeconds(2);

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The maximum number of events to emit in a single batch.
        /// </summary>
        private const int DefaultBatchSizeLimit = 50;

        public DatadogSink(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config, int? batchSizeLimit = null, TimeSpan? batchPeriod = null, Action<Exception> exceptionHandler = null, bool detectTCPDisconnection = false, IDatadogClient client = null)
            : base(batchSizeLimit ?? DefaultBatchSizeLimit, batchPeriod ?? DefaultBatchPeriod)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _client = client ?? CreateDatadogClient(apiKey, source, service, host, tags, config, detectTCPDisconnection, _cancellationToken);
            _exceptionHandler = exceptionHandler;
            _recycleResources = config.RecycleResources;
        }

        public DatadogSink(string apiKey, string source, string service, string host, string[] tags, DatadogConfiguration config, int queueLimit, int? batchSizeLimit = null, TimeSpan? batchPeriod = null, Action<Exception> exceptionHandler = null, bool detectTCPDisconnection = false, IDatadogClient client = null)
            : base(batchSizeLimit ?? DefaultBatchSizeLimit, batchPeriod ?? DefaultBatchPeriod, queueLimit)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _client = client ?? CreateDatadogClient(apiKey, source, service, host, tags, config, detectTCPDisconnection, _cancellationToken);
            _exceptionHandler = exceptionHandler;

        }

        public static DatadogSink Create(
            string apiKey,
            string source,
            string service,
            string host,
            string[] tags,
            DatadogConfiguration config,
            int? batchSizeLimit = null,
            TimeSpan? batchPeriod = null,
            int? queueLimit = null,
            Action<Exception> exceptionHandler = null,
            bool detectTCPDisconnection = false, IDatadogClient client = null)
        {
            if (queueLimit.HasValue)
                return new DatadogSink(apiKey, source, service, host, tags, config, queueLimit.Value, batchSizeLimit, batchPeriod, exceptionHandler, detectTCPDisconnection, client);

            return new DatadogSink(apiKey, source, service, host, tags, config, batchSizeLimit, batchPeriod, exceptionHandler, detectTCPDisconnection, client);
        }

        /// <summary>
        /// Emit a batch of log events to Datadog logs-backend.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>
        /// Only a single batch is able to be on the wire at a time. This ensures resources can be recycled per-batch.
        /// </remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                if (!events.Any())
                {
                    return;
                }
                if (_recycleResources)
                {
                    await Semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);
                }
                var logEvents = events.ToArray();
                await _client.WriteAsync(logEvents, _exceptionHandler).ConfigureAwait(false);
            } catch (Exception e)
            {
                if (e is OperationCanceledException && events.Any())
                {
                    OnException(new LogEventException("Datadog log sink was disposed", events.ToArray()));
                } else
                {
                    OnException(e);
                }
            } finally
            {
                if (_recycleResources)
                {
                    Semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // delay the dispose by one batch period so lingering events get logged. 
                    // after that the dispose thread will enter and block any further writes.
                    Task.Delay(DefaultBatchPeriod, _cancellationToken).Wait(_cancellationToken);
                    if (_recycleResources)
                    {
                        Semaphore.Wait(_cancellationToken);
                    }
                    _cancellationTokenSource.Cancel();
                    _client.Dispose();
                    base.Dispose(disposing);

                } finally
                {
                    if (_recycleResources)
                    {
                        Semaphore.Release();
                        Semaphore.Dispose();
                    }
                    _cancellationTokenSource.Dispose();
                }
            }
        }

        private static IDatadogClient CreateDatadogClient(string apiKey,
            string source,
            string service,
            string host,
            string[] tags,
            DatadogConfiguration configuration,
            bool detectTCPDisconnection,
            CancellationToken cancellationToken)
        {
            var logFormatter = new LogFormatter(source, service, host, tags, configuration.RecycleResources);
            if (configuration.UseTCP)
            {
                return new DatadogTcpClient(configuration, logFormatter, apiKey, detectTCPDisconnection, configuration.RecycleResources, cancellationToken);
            } else
            {
                return new DatadogHttpClient(configuration, logFormatter, apiKey, configuration.RecycleResources, cancellationToken);
            }
        }

        private void OnException(Exception e)
        {
            if (_exceptionHandler != null)
            {
                _exceptionHandler(e);
            }

            SelfLog.WriteLine("{0}", e.Message);
        }
    }
}
