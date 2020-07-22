using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Records;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Replies
{
    public class ResolveCallbackMessageTests
    {
        [Fact]
        public void ResolveCallbackMessageSerializationTest()
        {
            const string hostName = "RPI.local";
            const string fullName = "";
            const ushort port = 4567;
            const string txtRecordString = "mac=12:34:56:78:AA:BB";
            var txtRecord = TxtRecordBuilder.ParseString(txtRecordString);
            var txtRecordBytes = txtRecord.GetBytes();

            var sm = new ResolveCallbackMessage(CallbackMessageTestHelper.BaseValues, hostName, fullName, port, txtRecordBytes);

            var dm = CallbackMessageTestHelper.SerializeDeserializeTest(sm);

            dm.Payload.HostName.Should().Be(hostName);
            dm.Payload.FullName.Should().Be(fullName);
            dm.Payload.Port.Should().Be(port);
            dm.Payload.TxtRecord.Should().BeEquivalentTo(txtRecordBytes);
        }

        [Fact]
        public void ResolveCallbackMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ResolveCallbackMessage(new ServiceMessageHeader(OperationCode.SendBpf)));
        }
    }
}
