using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace DnsServiceDiscovery.Tests.Messages.Replies
{
    internal static class CallbackMessageTestHelper
    {
        public static readonly CallbackMessageBaseValues BaseValues = (ServiceFlags.Bogus, 20, ServiceError.ServiceNotRunning);

        internal static T SerializeDeserializeTest<T>(T message) where T : CallbackMessage
        {
            var dm = ServiceMessageTestHelper.SerializeDeserializeTest(message);

            dm.Payload.BaseValues.Should().Be(BaseValues);

            return dm;
        }
    }
}
