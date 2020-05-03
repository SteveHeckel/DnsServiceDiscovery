using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Communication;

namespace DnsServiceDiscovery.Tests.Communication
{
    internal class TestServiceTransportProvider : IServiceTransportProvider
    {
        private readonly ConcurrentDictionary<ushort, TestErrorReturnTransport> _errorReturnTransports = new ConcurrentDictionary<ushort, TestErrorReturnTransport>();

        private ushort NextErrorReturnTransportID
        {
            get
            {
                var id = (ushort)1;
                var keys = _errorReturnTransports.Keys;
                while (keys.Contains(id)) id++;
                return id;
            }
        }

        private readonly ServiceTransport _transport;

        public TestServiceTransportProvider()
        {
            var (remote, local) = Nerdbank.Streams.FullDuplexStream.CreatePair();
            Connector = new TestServiceTransportConnector(local);
            _transport = new ServiceTransport(remote, this);
        }

        public int ClientTimeout { get; set; } = 6000;
        public TestServiceTransportConnector Connector { get; set; }

        public Task<IServiceTransport> GetServiceTransport()
        {
            return Task.FromResult((IServiceTransport)_transport);
        }

        public IErrorReturnTransport GetErrorReturnTransport()
        {
            var ert = new TestErrorReturnTransport(NextErrorReturnTransportID);
            ert.Disposed += (s, e) =>
            {
                if (s is TestErrorReturnTransport dert && _errorReturnTransports.ContainsKey(dert.TransportID))
                    _errorReturnTransports.TryRemove(dert.TransportID, out dert);
            };

            _errorReturnTransports.TryAdd(ert.TransportID, ert);

            return ert;
        }

        internal IErrorReturnConnector GetErrorReturnConnector(ushort transportID)
        {
            if (_errorReturnTransports.ContainsKey(transportID))
            {
                return _errorReturnTransports[transportID];
            }

            return null;
        }
    }

    internal class TestServiceTransportException : Exception
    {
        public TestServiceTransportException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
