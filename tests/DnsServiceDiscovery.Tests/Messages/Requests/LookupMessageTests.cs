using System;
using System.Runtime.InteropServices.ComTypes;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;
using Xunit.Abstractions;

namespace DnsServiceDiscovery.Tests.Messages.Requests
{
    public class LookupMessageTests
    {
        private const string HostName = "TestHost";
        private const uint InterfaceIndex = 12;
        private const ProtocolFlags ProtoFlags = ProtocolFlags.IPv4v6;
        private const ServiceFlags Flags = ServiceFlags.ReturnIntermediates | ServiceFlags.Timeout;

        [Fact]
        public void LookupMessageSerializationTest()
        {
            var om = new LookupMessage(HostName, ProtoFlags, true, InterfaceIndex);

            var message = ServiceMessageTestHelper.SerializeDeserializeTest(om);

            message.Payload.HostName.Should().Be(HostName);
            message.Payload.ProtocolFlags.Should().Be(ProtoFlags);
            message.Payload.Flags.Should().Be(Flags);
            message.Payload.InterfaceIndex.Should().Be(InterfaceIndex);
        }

        [Fact]
        public void LookupMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new LookupMessage(new ServiceMessageHeader(OperationCode.BrowseRequest)));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new LookupMessage(null, ProtoFlags, false));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new LookupMessage(string.Empty, ProtoFlags, false));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new LookupMessage(HostName, ProtocolFlags.None, false));
            ServiceMessageTestHelper.ActionShouldNotThrow(() => new LookupMessage(HostName, ProtoFlags, false));
        }
    }
}
