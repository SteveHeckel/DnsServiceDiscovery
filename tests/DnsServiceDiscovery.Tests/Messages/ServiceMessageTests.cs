using System;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Xunit;
using FluentAssertions;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace DnsServiceDiscovery.Tests.Messages
{
    public class ServiceMessageTests
    {
        // The op code needs to be a message with no body
        private const OperationCode OpCode = OperationCode.CancelRequest;
        private const uint DataLength = uint.MaxValue;
        private const ServiceFlags Flags = ServiceFlags.Add;
        private const ulong SubordinateID = 0xDEADBEEF;
        private const ushort RegIndex = 0xBEAD;
        private const string RandomString = "Some Random Data";

        private readonly ServiceMessageHeader _testHeader = new ServiceMessageHeader(ServiceMessageHeader.CurrentVersion, DataLength, 
            Flags, OpCode, SubordinateID, RegIndex);

        [Fact]
        public void ServiceMessageHeaderSerializationTest()
        {
            var headerBytes = _testHeader.GetBytes();

            headerBytes.Length.Should().Be(ServiceMessageHeader.Length);

            Action act = () => ServiceMessageHeader.Parse(headerBytes, 1);
            act.Should().Throw<IndexOutOfRangeException>();

            var smh2 = ServiceMessageHeader.Parse(headerBytes, 0);

            smh2.Version.Should().Be(ServiceMessageHeader.CurrentVersion);
            smh2.DataLength.Should().Be(DataLength);
            smh2.Flags.Should().Be(Flags);
            smh2.OperationCode.Should().Be(OpCode);
            smh2.SubordinateID.Should().Be(SubordinateID);
            smh2.RegIndex.Should().Be(RegIndex);

            smh2.Should().BeEquivalentTo(_testHeader);
        }

        [Fact]
        public void ServiceMessageHeaderConstructionTest()
        {
            ServiceMessageTestHelper.ActionShouldThrow<ArgumentException>(() => new ServiceMessageHeader(OperationCode.None));
        }

        [Fact]
        public void ServiceMessageNoPayloadSerializationTest()
        {
            var sm = new ServiceMessage(_testHeader);

            ServiceMessageTestHelper.SerializeDeserializeTest(sm);
        }

        [Fact]
        public void ServiceMessageWithPayloadSerializationTest()
        {
            var sm = new ServiceMessage(_testHeader, new ServiceMessagePayload(Encoding.ASCII.GetBytes(RandomString)));

            ServiceMessageTestHelper.SerializeDeserializeTest(sm);
        }
    }

    public static class ServiceMessageTestHelper
    {
        internal static T SerializeDeserializeTest<T>(T message) where T : ServiceMessage
        {
            var messageBytes = message.GetBytes();

            var dm = ServiceMessageFactory.GetServiceMessage(messageBytes, 0);

            dm.Should().BeOfType<T>();
            dm.Should().BeEquivalentTo(message);

            return (T)dm;
        }

        internal static void ActionShouldThrow<TException>(Func<object> func) where TException : Exception
        {
            func.Should().Throw<TException>();
        }

        internal static void ActionShouldNotThrow(Func<object> func)
        {
            func.Should().NotThrow();
        }
    }
}
