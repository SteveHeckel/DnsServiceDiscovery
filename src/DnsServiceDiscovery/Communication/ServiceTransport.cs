using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Nito.AsyncEx;

namespace Mittosoft.DnsServiceDiscovery.Communication
{
    using ConnectionClosedCallback = Action<ConnectionClosedReason>;
    using ProcessReplyCallback = Action<CallbackMessage, bool>;

    internal enum ConnectionClosedReason
    {
        None,
        Faulted,
        Canceled,
        RemoteEndDisconnected,
        Incompatible
    }

    internal interface IServiceTransport
    {
        Task StartAsync(RequestMessage startMessage, ProcessReplyCallback processReplyCallback, ConnectionClosedCallback connectionClosedCallback);
        Task StopAsync();
        //bool IsConnected { get; }
        Task PostMessageAsync(ServiceMessage message);
        Task SendMessageAsync(RequestMessage message, bool separateErrorConnection);
    }

    internal class ServiceTransport : IServiceTransport
    {
        private readonly AsyncLock _writeMutex = new AsyncLock();

        private readonly Stream _stream;

        private Task _receiveLoopTask;
        private readonly CancellationTokenSource _receiveLoopCancellationTokenSource = new CancellationTokenSource();

        private ProcessReplyCallback _processReplyCallback;
        private ConnectionClosedCallback _connectionClosedCallback;
        private readonly IServiceTransportProvider _transportProvider;

        public ServiceTransport(Stream stream, IServiceTransportProvider transportProvider)
        {
            _stream = stream;
            _transportProvider = transportProvider;

            if (!IsConnected)
                throw new InvalidOperationException("The provided stream must be able to read and write");
        }

        public async Task StartAsync(RequestMessage startMessage, ProcessReplyCallback processReplyCallback, ConnectionClosedCallback connectionClosedCallback)
        {
            Guard.Against.Null(processReplyCallback, nameof(processReplyCallback));
            Guard.Against.Null(connectionClosedCallback, nameof(connectionClosedCallback));

            _processReplyCallback = processReplyCallback;
            _connectionClosedCallback = connectionClosedCallback;

            try
            {
                await SendMessageAsync(startMessage, false);
                _receiveLoopTask = ReceiveLoopAsync(_receiveLoopCancellationTokenSource.Token);
                var _ = _receiveLoopTask.ContinueWith(t => ReceiveLoopFaulted(t.Exception), default, 
                        TaskContinuationOptions.OnlyOnFaulted,
                        SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);
            }
            catch (DnsServiceException)
            {
                CloseConnection(ConnectionClosedReason.Faulted);
                throw;
            }
        }

        internal void ReceiveLoopFaulted(AggregateException ae)
        {
            // Todo: log these
            ae.Handle(exception => true );
            CloseConnection(ConnectionClosedReason.Faulted);
        }

        private void CloseConnection(ConnectionClosedReason reason)
        {
            _stream.Dispose();

            _connectionClosedCallback(reason);
        }

        public async Task StopAsync()
        {
            // Receive loop will close the client while exiting, it may have already exited
            // It also should've handled any exceptions
            if (_receiveLoopTask != null)
            {
                if (_receiveLoopTask.IsCompleted)
                    return;

                _receiveLoopCancellationTokenSource.Cancel();

                await _receiveLoopTask;
            }
        }

        public bool IsConnected => _stream.CanRead && _stream.CanWrite;

        public async Task PostMessageAsync(ServiceMessage message)
        {
            using (await _writeMutex.LockAsync())
            {
                if (!IsConnected)
                    throw new DnsServiceException(ServiceError.BadState);

                try
                {
                    var messageBytes = message.GetBytes();

                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();
                }
                catch (IOException)
                {
                    throw new DnsServiceException(ServiceError.ServiceNotRunning);
                }
                catch (ObjectDisposedException)
                {
                    throw new DnsServiceException(ServiceError.ServiceNotRunning);
                }
            }
        }

        public async Task SendMessageAsync(RequestMessage message, bool separateErrorConnection)
        {
            using (await _writeMutex.LockAsync())
            {
                if (!IsConnected)
                    throw new DnsServiceException(ServiceError.BadState);

                try
                {
                    IErrorReturnTransport ert = null;
                    if (separateErrorConnection)
                    {
                        ert = _transportProvider.GetErrorReturnTransport();
                        message.Payload.ErrorReturnPort = ert.StartListening();
                    }

                    var messageBytes = message.GetBytes();
                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();

                    if (separateErrorConnection)
                    {
                        var stream = await ert.AcceptConnection();
                        await GetOperationErrorAsync(stream);
                    }
                    else // This is only used if the operation is a primary connection
                        await GetOperationErrorAsync(_stream);
                }
                // If the read operation does not complete within the time specified by ReadTimeout, the read operation throws an IOException 
                catch (IOException)
                {
                    throw new DnsServiceException(ServiceError.ServiceNotRunning);
                }
                catch (ObjectDisposedException)
                {
                    throw new DnsServiceException(ServiceError.ServiceNotRunning);
                }
            }
        }

        // This will be called prior to the receive loop being executed on a primary connection
        // Or on a separate error return socket/stream for a subordinate
        internal async Task GetOperationErrorAsync(Stream stream)
        {
            int saveTimeout = Timeout.Infinite;

            if (stream.CanTimeout)
            {
                saveTimeout = stream.ReadTimeout;
                stream.ReadTimeout = _transportProvider.ClientTimeout;
            }

            var errorBytes = new byte[sizeof(uint)];
            await ReadAllAsync(stream, errorBytes, errorBytes.Length, default);
            var error = (ServiceError)ServiceMessage.NetworkToHostOrder(BitConverter.ToUInt32(errorBytes, 0));

            if (stream.CanTimeout)
                stream.ReadTimeout = saveTimeout;

            if (error != ServiceError.NoError)
                throw new DnsServiceException(error);

            // Let Exceptions be handled by caller
        }

        private static async Task ReadAllAsync(Stream stream, byte[] buffer, int count, CancellationToken token)
        {
            var index = 0;
            while (count != 0)
            {
                token.ThrowIfCancellationRequested();
                var numRead = await stream.ReadAsync(buffer, index, count, token);
                if (numRead == 0) // remote end closed probably
                    throw new DnsServiceException(ServiceError.ServiceNotRunning);
                index += numRead;
                count -= numRead;
            }
        }

        private bool DataAvailable
        {
            get
            {
                if (_stream is NetworkStream networkStream)
                    return networkStream.DataAvailable;

                return false;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            if (_stream.CanTimeout)
                _stream.ReadTimeout = Timeout.Infinite;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    var headerBytes = new byte[ServiceMessageHeader.Length];
                    await ReadAllAsync(_stream, headerBytes, headerBytes.Length, token);
                    var header = ServiceMessageHeader.Parse(headerBytes, 0);

                    if (header.Version != ServiceMessageHeader.CurrentVersion)
                    {
                        CloseConnection(ConnectionClosedReason.Incompatible);
                        return;
                    }

                    var payloadBytes = new byte[header.DataLength];
                    if (header.DataLength != 0)
                        await ReadAllAsync(_stream, payloadBytes, payloadBytes.Length, token);

                    var cbm = ServiceMessageFactory.GetCallbackMessage(header, payloadBytes, 0);
                    try
                    {
                        _processReplyCallback(cbm, DataAvailable);
                    }
                    catch (Exception)
                    {
                        CloseConnection(ConnectionClosedReason.Incompatible);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    CloseConnection(ConnectionClosedReason.Canceled);
                    return;
                }
                catch (IOException)
                {
                    CloseConnection(ConnectionClosedReason.RemoteEndDisconnected);
                    return;
                }
                catch (DnsServiceException)
                {
                    CloseConnection(ConnectionClosedReason.RemoteEndDisconnected);
                    return;
                }
            }

            CloseConnection(ConnectionClosedReason.Canceled);
        }
    }
}
