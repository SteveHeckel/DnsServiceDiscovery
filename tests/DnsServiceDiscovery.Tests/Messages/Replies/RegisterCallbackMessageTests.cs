using System;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Xunit;

namespace DnsServiceDiscovery.Tests.Messages.Replies
{
    public class RegisterCallbackMessageTests
    {
        [Fact]
        public void RegisterCallbackMessageSerializationTest()
        {
            const string servicetype = "_cac.tcp";
            const string domain = "local.";
            const string instanceName = "RPI.local";

            var sm = new RegisterCallbackMessage(CallbackMessageTestHelper.BaseValues, instanceName, servicetype, domain);

            var dm = CallbackMessageTestHelper.SerializeDeserializeTest(sm);

            dm.Payload.InstanceName.Should().Be(instanceName);
            dm.Payload.ServiceType.Should().Be(servicetype);
            dm.Payload.Domain.Should().Be(domain);
        }

        [Fact]
        public void RegisterCallbackMessageConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new RegisterCallbackMessage(new ServiceMessageHeader(OperationCode.SendBpf)));
        }
    }
}
