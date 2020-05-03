using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DnsServiceDiscovery.Tests.Communication;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Communication
{
    public class ServiceTransportTests
    {
        private readonly TestServiceTransportProvider _provider = new TestServiceTransportProvider();
        private readonly IServiceTransport _transport;

        public ServiceTransportTests()
        {
            _transport = _provider.GetServiceTransport().Result;
        }

        [Fact]
        public async Task ServiceTransportStartTest()
        {
            var cm = new RequestMessage(OperationCode.ConnectionRequest);
            ServiceMessage rm = null;
            var aConnectionClosed = false;

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            void ProcessCallbackMessage(CallbackMessage message, bool moreComing)
            {
            }

            void ServiceTransportConnectionClosed(ConnectionClosedReason reason)
            {
                aConnectionClosed = true;
            }

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            async void ProcessRequestMessage(RequestMessage message)
            {
                rm = message;
                await _provider.Connector.SendToRemote(ServiceError.NoError);
            }

            void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
            {
                aConnectionClosed = true;
            }

            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            await _transport.StartAsync(cm, ProcessCallbackMessage, ServiceTransportConnectionClosed);

            if (aConnectionClosed)
                throw new Exception("A connection was closed unexpectedly");
           
            cm.Should().BeEquivalentTo(rm);
        }

        [Fact]
        public void ServiceTransportStartFailDueToCloseTest()
        {
            var cm = new RequestMessage(OperationCode.ConnectionRequest);

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            void ProcessCallbackMessage(CallbackMessage message, bool moreComing)
            {
            }

            void ServiceTransportConnectionClosed(ConnectionClosedReason reason)
            {
            }

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            async void ProcessRequestMessage(RequestMessage message)
            {
                await _provider.Connector.StopAsync();
            }

            void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
            {
            }

            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            Func<Task> func = async () => await _transport.StartAsync(cm, ProcessCallbackMessage, ServiceTransportConnectionClosed);

            func.Should().Throw<DnsServiceException>();
        }
    
        [Fact]
        public void ServiceTransportStartFailDueToErrorTest()
        {
            var cm = new RequestMessage(OperationCode.ConnectionRequest);

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            void ProcessCallbackMessage(CallbackMessage message, bool moreComing)
            {
            }

            void ServiceTransportConnectionClosed(ConnectionClosedReason reason)
            {
            }

            // Called by ReceiveLoop, handle exceptions
            // Aka don't assert in these
            async void ProcessRequestMessage(RequestMessage message)
            {
                await _provider.Connector.SendToRemote(ServiceError.BadState);
            }

            void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
            {
            }

            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            Func<Task> func = async () => await _transport.StartAsync(cm, ProcessCallbackMessage, ServiceTransportConnectionClosed);

            func.Should().Throw<DnsServiceException>();
        }
    }

    internal class ServiceTransportTestHelper
    {
        private readonly TestServiceTransportProvider _provider = new TestServiceTransportProvider();
        private readonly IServiceTransport _transport;
        internal Func<ConnectionClosedReason, bool> AlternateConnectorClosedHandler { get; set; }
        internal Func<RequestMessage, Task> AlternateProcessRequestMessageHandler { get; set; }
        private bool _connectionClosed = false;

        public ServiceTransportTestHelper()
        {
            _transport = _provider.GetServiceTransport().Result;
        }

        public void Execute(Action<TestServiceTransportProvider, IServiceTransport> action, Action assertAction, bool throwIfConnectionClosed = false)
        {
            var cm = new RequestMessage(OperationCode.ConnectionRequest);

            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            Func<Task> func = async () => await _transport.StartAsync(cm, ProcessCallbackMessage, ServiceTransportConnectionClosed);

            action?.Invoke(_provider, _transport);

            if (throwIfConnectionClosed)
                ThrowIfConnectionClosed();
            
            assertAction();

            //cm.Should().BeEquivalentTo(rm);
        }

        private void ThrowIfConnectionClosed()
        {
            if (_connectionClosed)
                throw new Exception("The service transport connector was closed unexpectedly");
        }

        // Called by ReceiveLoop, handle exceptions
        // Aka don't assert in these
        void ProcessCallbackMessage(CallbackMessage message, bool moreComing)
        {
        }

        void ServiceTransportConnectionClosed(ConnectionClosedReason reason)
        {
            _connectionClosed = true;
        }

        // Called by ReceiveLoop, handle exceptions
        // Aka don't assert in these
        private async void ProcessRequestMessage(RequestMessage message)
        {
            if (AlternateProcessRequestMessageHandler != null)
                await AlternateProcessRequestMessageHandler(message);
            else
            {
                // See if it's a subordinate request, aka has error return port value in payload
                if (message.Payload?.ErrorReturnPort != null)
                {
                    var ert = _provider.GetErrorReturnConnector(message.Payload.ErrorReturnPort.Value);
                    await ert.SendToRemote(ServiceError.NoError);
                }
                else
                    await _provider.Connector.SendToRemote(ServiceError.NoError);
            }
        }

        private void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
        {
            _connectionClosed = AlternateConnectorClosedHandler == null || AlternateConnectorClosedHandler(reason);
        }
    }
}
