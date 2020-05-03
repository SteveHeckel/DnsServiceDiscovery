using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DnsServiceDiscovery.Tests.Messages;
using Microsoft;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages;
using Nerdbank.Streams;

namespace DnsServiceDiscovery.Tests.Communication
{
    internal interface IErrorReturnConnector
    {
        Task SendToRemote(ServiceError error);
        Task SendToRemote(byte[] message);
    }

    internal class TestErrorReturnTransport : IErrorReturnTransport, IErrorReturnConnector
    {
        public ushort TransportID { get; }
        public Stream LocalStream { get; }
        public Stream RemoteStream { get; }

        public TestErrorReturnTransport(ushort transportID)
        {
            TransportID = transportID;
            (LocalStream, RemoteStream) = FullDuplexStream.CreatePair();
        }

        public ushort StartListening()
        {
            return TransportID;
        }

        public Task<Stream> AcceptConnection()
        {
            return Task.FromResult(RemoteStream);
        }

        public Task SendToRemote(ServiceError error)
        {
            return SendToRemote(BitConverter.GetBytes(ServiceMessage.HostToNetworkOrder((uint) error)));
        }

        public async Task SendToRemote(byte[] message)
        {
            try
            {
                await LocalStream.WriteAsync(message, 0, message.Length);
                await LocalStream.FlushAsync();
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

        public event EventHandler Disposed;

        private void OnDisposed()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            LocalStream?.Dispose();
            RemoteStream?.Dispose();
        }
    }
}
