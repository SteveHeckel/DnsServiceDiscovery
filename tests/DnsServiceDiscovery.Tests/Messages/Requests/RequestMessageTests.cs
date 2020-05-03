using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Requests
{
    public class RequestMessageTests
    {
        private const string Servicetype = "_cac.tcp";
        private const string Domain = "local.";
        private const uint InterfaceIndex = 12;
        private const ushort Port = 3456;

        [Fact]
        public void RequestMessageWithErrorReturnPortSerializationTest()
        {
            var sm = new BrowseMessage(Servicetype, Domain, InterfaceIndex);
            sm.SetSubordinateID(1); // Marks message as subordinate

            // Subordinate message with no ErrorReturnPort value set should throw on serialization
            Action act = () => sm.GetBytes();
            act.Should().Throw<InvalidOperationException>();

            sm.Payload.ErrorReturnPort = Port;

            var bm = ServiceMessageTestHelper.SerializeDeserializeTest(sm);

            bm.Payload.ErrorReturnPort.Should().Be(Port);
        }
    }
}