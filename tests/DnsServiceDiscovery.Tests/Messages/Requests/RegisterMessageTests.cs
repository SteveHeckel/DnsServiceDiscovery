using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Mittosoft.DnsServiceDiscovery.Records;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Requests
{
    public class RegisterMessageTests
    {
        private const string InstanceName = "RPI.Local";
        private const string HostName = "TestHost";
        private const string Servicetype = "_cac.tcp";
        private const string Domain = "local.";
        private const uint InterfaceIndex = 12;
        private const string TxtRecordString = "mac=12:34:56:78:AA:BB";
        private readonly byte[] _txtRecordBytes;

        public RegisterMessageTests()
        {
            var trb = new TxtRecordBuilder(TxtRecordString);
            _txtRecordBytes = trb.GetBytes();
        }

        [Fact]
        public void RegisterMessageSerializationTest()
        {
            var om = new RegisterMessage(InstanceName, Servicetype, Domain, HostName, ushort.MaxValue, _txtRecordBytes, ServiceFlags.Bogus, InterfaceIndex);

            var message = ServiceMessageTestHelper.SerializeDeserializeTest(om);

            message.Payload.InstanceName.Should().Be(InstanceName);
            message.Payload.HostName.Should().Be(HostName);
            message.Payload.ServiceType.Should().Be(Servicetype);
            message.Payload.Domain.Should().Be(Domain);
            message.Payload.TxtRecord.Should().BeEquivalentTo(_txtRecordBytes);
            message.Payload.Flags.Should().Be(ServiceFlags.Bogus);
            message.Payload.InterfaceIndex.Should().Be(InterfaceIndex);
        }

        [Fact]
        public void RegisterMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new RegisterMessage(new ServiceMessageHeader(OperationCode.SendBpf)));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new RegisterMessage(InstanceName, null, Domain, HostName, 3456, _txtRecordBytes));
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new RegisterMessage(InstanceName, string.Empty, Domain, HostName, 3456, _txtRecordBytes));

            ServiceMessageTestHelper.ActionShouldNotThrow(() => new RegisterMessage(InstanceName, Servicetype, Domain, HostName, 3456, _txtRecordBytes));

        }
    }
}
