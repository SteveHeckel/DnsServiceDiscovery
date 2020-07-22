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
    public class BrowseOperationTests
    {
        private readonly OperationTestHelper _helper = new OperationTestHelper();

        //[Fact]
        public async Task BrowseOperationExecuteAsPrimaryTest()
        {
            await _helper.PerformOperationExecuteTestAsPrimary(new BrowseOperation("_cac._tcp"));
        }

        //[Fact]
        public async Task BrowseOperationExecuteAsSubordinateTest()
        {
            await _helper.PerformOperationExecuteTestAsSubordinate(new BrowseOperation("_cac._tcp"));
        }
    }
}
