using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mittosoft.DnsServiceDiscovery.Communication
{
    internal interface IErrorReturnTransport : IDisposable
    {
        ushort StartListening();
        Task<Stream> AcceptConnection();
    }

    internal class ErrorReturnTransport : IErrorReturnTransport
    {
        private readonly IServiceTransportProvider _transportProvider;
        private readonly TcpListener _listener;

        public ErrorReturnTransport(IServiceTransportProvider transportProvider)
        {
            _transportProvider = transportProvider;
            _listener = new TcpListener(IPAddress.Parse(ServiceTransportProvider.LocalHost), 0);
        }

        public ushort StartListening()
        {
            try
            {
                _listener.Start(1);
                var endPoint = (IPEndPoint)_listener.LocalEndpoint;

                return (ushort)endPoint.Port;
            }
            catch (SocketException se)
            {
                throw new DnsServiceException(se);
            }
        }

        public async Task<Stream> AcceptConnection()
        {
            var acceptTask = _listener.AcceptTcpClientAsync();
            var timeoutTask = Task.Delay(_transportProvider.ClientTimeout);

            var completedTask = await Task.WhenAny(acceptTask, timeoutTask);

            // There may be a race condition here (and a socket leak) if the 'accept' task completes
            // at/about the same time the timeout task completes, but I don't think it's worth worrying about.
            // If the serice hasn't connected back in the very-long-timeout time then it probably isn't going
            // to happen at all.  An alternative would be to check the acceptTask.Status == TaskStatus.RanToCompletetion
            // -----------------
            // Stop closes the listener. Any unaccepted connection requests in the queue will be lost
            // This should put the acceptTask in the 'Faulted' state if it hasn't already completed
            _listener.Stop();

            if (completedTask == acceptTask)
            {
                var client = await acceptTask;
                return client.GetStream();
            }

            throw new DnsServiceException(ServiceError.Timeout);
        }

        public void Dispose()
        {
        }
    }
}
