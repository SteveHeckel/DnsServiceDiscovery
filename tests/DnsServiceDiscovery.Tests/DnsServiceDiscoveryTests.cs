using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using DnsServiceDiscovery.Tests.Communication;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Mittosoft.DnsServiceDiscovery.Operations;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace DnsServiceDiscovery.Tests
{
    using DnsServiceDiscovery = Mittosoft.DnsServiceDiscovery.DnsServiceDiscovery;

    public class DnsServiceDiscoveryTests
    {
        [Theory]
        [MemberData(nameof(GetMyData), 0, false)] // Zero (or less) means "all data elements" to the GetData method
        public async Task DnsServiceDiscoveryTheory(Func<IDnsServiceDiscovery, Task<IOperationToken>> testFunc)
        {
            var helper = new ServiceTestHelper();

            await helper.PerformServiceTest(testFunc);
        }

        public static IEnumerable<object[]> GetMyData(int numTests, bool includeCallbackMessage = false)
        {
            var allData = new List<object[]>
            {
                new object[]
                {
                    new Func<IDnsServiceDiscovery, Task<IOperationToken>>(async (service) => await service.BrowseAsync("_cac._tcp")),
                    new BrowseCallbackMessage(ServiceFlags.Add, "NewInstance", "_cac._tcp", "MyDomain")
                },
                new object[]
                {
                    new Func<IDnsServiceDiscovery, Task<IOperationToken>>(async (service) => await service.ResolveAsync("Blah", "_cac._tcp", "MyDomain")),
                    new ResolveOperation("Blah", "_cac._tcp", "MyDomain"),
                },
                new object[] {
                    new Func<IDnsServiceDiscovery, Task<IOperationToken>>(async (service) => await service.LookupAsync("FooHost", ProtocolFlags.IPv4, true)),
                    new LookupCallbackMessage(ServiceFlags.Add, "FooHost", ResourceRecordType.A, 1,
                        IPAddress.Parse("127.0.0.1").GetAddressBytes(), 42)
                },
                new object[]
                {
                    new Func<IDnsServiceDiscovery, Task<IOperationToken>>(async (service) => await service.RegisterAsync("Blah", "_cac._tcp", "MyDomain", null, 6789)),
                    new RegisterCallbackMessage(ServiceFlags.Add, "Blah", "_cac._tcp", "MyDomain"),
                },
            };

            if (numTests <= 0)
                numTests = allData.Count;

            return allData.Select(e => includeCallbackMessage ? new[] { e[0], e[1] } : new[] { e[0] }).Take(numTests);
        }

    }

    internal class ServiceTestHelper
    {
        private readonly TestServiceTransportProvider _provider = new TestServiceTransportProvider();
        internal Func<ConnectionClosedReason, bool> AlternateConnectorClosedHandler { get; set; }
        internal Func<RequestMessage, Task> AlternateProcessRequestMessageHandler { get; set; }
        private bool _connectorClosed;
        private readonly CallbackMessage _callbackMessageUponRequest = null;

        internal async Task<IOperationToken> PerformServiceTest(Func<IDnsServiceDiscovery, Task<IOperationToken>> testFunc)
        {
            Guard.Against.Null(testFunc, nameof(testFunc));

            DnsServiceDiscovery.SetServiceTransportProvider(_provider);
            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            var service = new DnsServiceDiscovery();;

            var iot = await testFunc(service);

            ThrowIfConnectorClosed();
            iot.State.Should().Be(OperationState.Executing);

            return iot;
        }

        private void ThrowIfConnectorClosed()
        {
            if (_connectorClosed)
                throw new Exception("The service transport connector was closed unexpectedly");
        }

        // Called by ReceiveLoop, handle exceptions
        // Aka don't assert in these
        private async void ProcessRequestMessage(RequestMessage message)
        {
            if (message.Header.OperationCode == OperationCode.CancelRequest)
                return;

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

                if (_callbackMessageUponRequest != null)
                {
                    if (message.Header.SubordinateID != 0)
                        _callbackMessageUponRequest.SetSubordinateID(message.Header.SubordinateID);
                    await _provider.Connector.SendToRemote(_callbackMessageUponRequest.GetBytes());
                }
            }
        }

        private void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
        {
            _connectorClosed = AlternateConnectorClosedHandler == null || AlternateConnectorClosedHandler(reason);
        }

    }
}
