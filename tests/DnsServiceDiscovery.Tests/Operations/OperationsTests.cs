using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Operations;
using Xunit;
using ResolveEventArgs = System.ResolveEventArgs;

namespace DnsServiceDiscovery.Tests.Operations
{
    public class OperationsTests
    {
        [Theory]
        [MemberData(nameof(GetData), 0, false)] // Zero (or less) means "all data elements" to the GetData method
        internal async Task OperationExecuteAsPrimaryTheory(OperationBase op)
        {
            var helper = new OperationTestHelper();

            await helper.PerformOperationExecuteTestAsPrimary(op);
        }

        [Theory]
        [MemberData(nameof(GetData), 4, false)]
        internal async Task OperationExecuteAsSubordinateTheory(OperationBase op)
        {
            var helper = new OperationTestHelper();

            await helper.PerformOperationExecuteTestAsSubordinate(op);
        }

        [Theory]
        [MemberData(nameof(GetData), 4, true)]
        internal async Task OperationExecuteAsSubordinateWithCallbacks(OperationBase op, CallbackMessage cbMessage)
        {
            var helper = new OperationTestHelper();

            await helper.PerformOperationExecuteTestAsSubordinateWithCallback(op, cbMessage);
        }

        public static IEnumerable<object[]> GetData(int numTests, bool includeCallbackMessage = false)
        {
            var allData = new List<object[]>
            {
                new object[]
                {
                    new BrowseOperation("_cac._tcp"),
                    new BrowseCallbackMessage(ServiceFlags.Add, "NewInstance", "_cac._tcp", "MyDomain")
                },
                new object[]
                {
                    new ResolveOperation("Blah", "_cac._tcp", "MyDomain"),
                    new ResolveCallbackMessage("FooHost", "_cac._tcp.local.", 6543),
                },
                new object[] {
                    new LookupOperation("MyHostName", ProtocolFlags.IPv4v6, true),
                    new LookupCallbackMessage(ServiceFlags.Add, "FooHost", ResourceRecordType.A, 1,
                        IPAddress.Parse("127.0.0.1").GetAddressBytes(), 42)
                },
                new object[]
                {
                    new RegisterOperation("Blah", "_cac._tcp", null, null, 5678),
                    new RegisterCallbackMessage(ServiceFlags.Add, "NewInstance", "_cac._tcp", "MyDomain"),
                },
                new object[] { new ConnectionOperation(), }
            };

            if (numTests <= 0)
                numTests = allData.Count;

            return allData.Select(e => includeCallbackMessage ? new[] { e[0], e[1] } : new[] { e[0] }).Take(numTests);
        }

        private void BrowsEventHandler(object sender, BrowseEventArgs args)
        {
        }

        private void RegisterationEventHandler(object sender, RegistrationEventArgs args)
        {
        }

        private void ResolveEventHandler(object esnder, ResolveEventArgs args)
        {
        }

        private void LookupEventHandler(object sender, LookupEventArgs args)
        {
        }
    }
}
