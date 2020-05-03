using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mittosoft.DnsServiceDiscovery.Communication
{
    internal interface IServiceTransportProvider
    {
        int ClientTimeout { get; }

        Task<IServiceTransport> GetServiceTransport();
        IErrorReturnTransport GetErrorReturnTransport();
    }

    internal class ServiceTransportProvider : IServiceTransportProvider
    {
        public const string LocalHost = "127.0.0.1";
        private const int ServerPort = 5354;
        public static int ConnectTries = 4;
        public int ClientTimeout => 60000; // 1 minute
        
        public async Task<IServiceTransport> GetServiceTransport()
        {
            var client = await ConnectToServerAsync();
            return new ServiceTransport(client.GetStream(), this);
        }

        public IErrorReturnTransport GetErrorReturnTransport()
        {
            return new ErrorReturnTransport(this);
        }

        private static async Task<TcpClient> ConnectToServerAsync()
        {
            var client = new TcpClient();

            for (var i = 0; i < ConnectTries; i++)
            {
                try
                {
                    await client.ConnectAsync(LocalHost, ServerPort);
                    break;
                }
                // Comment from Bonjour source - dnssd\dnssd_clientstub.c
                // If we failed, then it may be because the daemon is still launching.
                // This can happen for processes that launch early in the boot process, while the
                // daemon is still coming up. Rather than fail here, we wait 1 sec and try again.
                // If, after DNSSD_CLIENT_MAXTRIES, we still can't connect to the daemon,
                // then we give up and return a failure code.
                catch (SocketException se) when (se.ErrorCode == (int)SocketError.TimedOut ||
                                                 se.ErrorCode == (int)SocketError.ConnectionRefused)
                {
                    await Task.Delay(1000);
                }
            }

            if (!client.Connected)
                throw new DnsServiceException(ServiceError.ServiceNotRunning);

            return client;
        }
    }
}
