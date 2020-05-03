using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Requests
{
    public class BrowseMessageTests
    {
        private const string Servicetype = "_cac.tcp";
        private const string Domain = "local.";
        private const uint InterfaceIndex = 12;

        [Fact]
        public void BrowseMessageSerializationTest()
        {
            var sm = new BrowseMessage(Servicetype, Domain, InterfaceIndex);

            var bm = ServiceMessageTestHelper.SerializeDeserializeTest(sm);

            bm.Payload.ServiceType.Should().Be(Servicetype);
            bm.Payload.Domain.Should().Be(Domain);
            bm.Payload.InterfaceIndex.Should().Be(InterfaceIndex);
        }

        [Fact]
        public void BrowseMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new BrowseMessage(new ServiceMessageHeader(OperationCode.RegisterServiceReply)));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new BrowseMessage(null, null, 0));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new BrowseMessage("", null, 0));
            ServiceMessageTestHelper.ActionShouldNotThrow(() => new BrowseMessage(Servicetype, null, 0));
        }
    }
}
