using System;
using System.Net;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Replies
{
    public class LookupCallbackMessageTests
    {
        [Fact]
        public void LookupCallbackMessageSerializationTest()
        {
            const string hostName = "RPI.local";
            const ResourceRecordType rrType = ResourceRecordType.A;
            const ushort rrClass = 1; // IN
            var rrData = IPAddress.Parse("127.0.0.1").GetAddressBytes();
            const uint ttl = 15;

            var sm = new LookupCallbackMessage(CallbackMessageTestHelper.BaseValues, hostName, rrType, rrClass, rrData, ttl);

            var dm = CallbackMessageTestHelper.SerializeDeserializeTest(sm);

            dm.Payload.HostName.Should().Be(hostName);
            dm.Payload.RecordType.Should().Be(rrType);
            dm.Payload.RecordClass.Should().Be(rrClass);
            dm.Payload.RecordData.Should().BeEquivalentTo(rrData);
            dm.Payload.TimeToLive.Should().Be(ttl);
        }

        [Fact]
        public void LookupCallbackMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new LookupCallbackMessage(new ServiceMessageHeader(OperationCode.SendBpf)));
        }
    }
}
