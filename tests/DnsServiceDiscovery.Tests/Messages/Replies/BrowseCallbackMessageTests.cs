using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Replies
{
    public class BrowseCallbackMessageTests
    {
        [Fact]
        public void BrowseCallbackMessageSerializationTest()
        {
            const string servicetype = "_cac.tcp";
            const string domain = "local.";
            const string instanceName = "RPI.local";

            var sm = new BrowseCallbackMessage(CallbackMessageTestHelper.BaseValues, instanceName, servicetype, domain);

            var bm = CallbackMessageTestHelper.SerializeDeserializeTest(sm);

            bm.Payload.InstanceName.Should().Be(instanceName);
            bm.Payload.ServiceType.Should().Be(servicetype);
            bm.Payload.Domain.Should().Be(domain);
        }

        [Fact]
        public void BrowseCallbackMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new BrowseCallbackMessage(new ServiceMessageHeader(OperationCode.SendBpf)));
        }
    }
}