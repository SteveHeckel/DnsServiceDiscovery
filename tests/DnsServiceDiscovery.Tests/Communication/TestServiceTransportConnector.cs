using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Nerdbank.Streams;
using Nito.AsyncEx;

namespace DnsServiceDiscovery.Tests.Communication
{
    using ConnectionClosedCallback = Action<ConnectionClosedReason>;
    using ProcessRequestCallback = Action<RequestMessage>;

    internal class TestServiceTransportConnector
    {
        private readonly Stream _stream;

        private Task _receiveLoopTask;
        private readonly CancellationTokenSource _receiveLoopCancellationTokenSource = new CancellationTokenSource();
        private ConnectionClosedCallback _connectionClosedCallback;
        private ProcessRequestCallback _processRequestCallback;

        public TestServiceTransportConnector(Stream stream)
        {
            _stream = stream;
        }

        public void Start(ProcessRequestCallback processRequestCallback, ConnectionClosedCallback connectionClosedCallback)
        {
            _processRequestCallback = processRequestCallback;
            _connectionClosedCallback = connectionClosedCallback;

            _receiveLoopTask = ReceiveLoopAsync(_receiveLoopCancellationTokenSource.Token);
            var _ = _receiveLoopTask.ContinueWith(t => ReceiveLoopFaulted(t.Exception), default,
                TaskContinuationOptions.OnlyOnFaulted, SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext()
                    : TaskScheduler.Current);
        }

        internal void ReceiveLoopFaulted(AggregateException ae)
        {
            // Todo: log these
            ae.Handle(exception => true);
            CloseConnection(ConnectionClosedReason.Faulted);
        }

        public async Task StopAsync()
        {
            _receiveLoopCancellationTokenSource.Cancel();
            // Receive loop will close the client while exiting, it may have already exited
            // It also should've handled any exceptions
            if (_receiveLoopTask != null)
            {
                try
                {
                    await _receiveLoopTask;
                }
                catch (Exception)
                {
                    // Todo: log exception
                }
            }
        }

        public bool IsConnected => _stream.CanRead && _stream.CanWrite;
        
        private readonly AsyncLock _writeMutex = new AsyncLock();

        public Task SendToRemote(ServiceError error)
        {
            return SendToRemote(BitConverter.GetBytes(ServiceMessage.HostToNetworkOrder((uint)error)));
        }

        public async Task SendToRemote(byte[] message)
        {
            try
            {
                await _stream.WriteAsync(message, 0, message.Length);
                await _stream.FlushAsync();
            }
            catch (IOException e)
            {
                throw new TestServiceTransportException("Except while writing to error stream", e);
            }
            catch (ObjectDisposedException e)
            {
                throw new TestServiceTransportException("Except while writing to error stream", e);
            }
        }
        
        public async Task PostMessageAsync(CallbackMessage message)
        {
            using (await _writeMutex.LockAsync())
            {
                if (!IsConnected)
                    throw new TestServiceTransportException("Stream is not connected");

                try
                {
                    var messageBytes = message.GetBytes();

                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();
                }
                catch (IOException e)
                {
                    throw new TestServiceTransportException("Except while writing to remote stream", e);
                }
                catch (ObjectDisposedException e)
                {
                    throw new TestServiceTransportException("Except while writing to remote stream", e);
                }
            }
        }
        
        private void CloseConnection(ConnectionClosedReason reason)
        {
            _stream.Dispose();

            _connectionClosedCallback(reason);
        }

        private static async Task ReadAllAsync(Stream stream, byte[] buffer, int count, CancellationToken token)
        {
            var index = 0;
            while (count != 0)
            {
                token.ThrowIfCancellationRequested();
                var numRead = await stream.ReadAsync(buffer, index, count, token);
                if (numRead == 0) // remote end closed probably
                    throw new TestServiceTransportException("Remote end disconnected");
                index += numRead;
                count -= numRead;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
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

                    var sm = ServiceMessageFactory.GetRequestMessage(header, payloadBytes, 0);
                    try
                    {
                        _processRequestCallback(sm);
                    }
                    catch (Exception)
                    {
                        // Todo: log and/or raise an event
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
                catch (TestServiceTransportException)
                {
                    CloseConnection(ConnectionClosedReason.RemoteEndDisconnected);
                    return;
                }
            }

            CloseConnection(ConnectionClosedReason.Canceled);
        }
    }
}
