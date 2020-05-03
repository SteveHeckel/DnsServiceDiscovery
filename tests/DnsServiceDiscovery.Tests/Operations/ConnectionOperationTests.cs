using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DnsServiceDiscovery.Tests.Communication;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Mittosoft.DnsServiceDiscovery.Operations;
using Xunit;

namespace DnsServiceDiscovery.Tests.Operations
{
    public class ConnectionOperationTests
    {
        private readonly OperationTestHelper _helper = new OperationTestHelper();

        //[Fact]
        public async Task ConnectionOperationExecuteTest()
        {
            await _helper.PerformOperationExecuteTestAsPrimary(new ConnectionOperation());
        }
    }
}
