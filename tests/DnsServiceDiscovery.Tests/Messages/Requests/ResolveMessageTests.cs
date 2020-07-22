using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Requests
{
    public class ResolveMessageTests
    {
        private const string InstanceName = "RPI.Local";
        private const string ServiceType = "_cac.tcp";
        private const string Domain = "local.";
        private const uint InterfaceIndex = 12;

        [Fact]
        public void ResolveMessageSerializationTest()
        {
            var om = new ResolveMessage(InstanceName, ServiceType, Domain, InterfaceIndex);

            var dm = ServiceMessageTestHelper.SerializeDeserializeTest(om);

            dm.Payload.InstanceName.Should().Be(InstanceName);
            dm.Payload.ServiceType.Should().Be(ServiceType);
            dm.Payload.Domain.Should().Be(Domain);
            dm.Payload.InterfaceIndex.Should().Be(InterfaceIndex);
        }

        [Fact]
        public void ResolveMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(new ServiceMessageHeader(OperationCode.SendBpf)));

            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(null, ServiceType, Domain, 0));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(string.Empty, ServiceType, Domain, 0));

            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(InstanceName, null, Domain, 0));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(InstanceName, string.Empty, Domain, 0));

            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(InstanceName, ServiceType, null, 0));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveMessage(InstanceName, ServiceType, string.Empty, 0));

            ServiceMessageTestHelper.ActionShouldNotThrow(() => new ResolveMessage(InstanceName, ServiceType, Domain, 0));
        }
    }
}
