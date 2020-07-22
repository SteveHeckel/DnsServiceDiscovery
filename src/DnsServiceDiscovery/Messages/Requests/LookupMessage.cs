using System;
using System.IO;
using Mittosoft.DnsServiceDiscovery.Helpers;

namespace Mittosoft.DnsServiceDiscovery.Messages.Requests
{
    internal class LookupMessage : RequestMessage<LookupMessagePayload>
    {
        public LookupMessage(ServiceMessageHeader header) : this(header, new LookupMessagePayload())
        {
        }

        public LookupMessage(string hostName, ProtocolFlags protocolFlags, bool withTimeout, uint interfaceIndex = 0)
            : this(new ServiceMessageHeader(OperationCode.AddressInfoRequest),
                new LookupMessagePayload(hostName, protocolFlags, ServiceFlags.ReturnIntermediates | (withTimeout ? ServiceFlags.Timeout : ServiceFlags.None),
                    interfaceIndex))
        {
        }

        public LookupMessage(ServiceMessageHeader header, LookupMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.AddressInfoRequest)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class LookupMessagePayload : RequestMessagePayload
    {
        public string HostName { get; private set; } = string.Empty;
        public ProtocolFlags ProtocolFlags { get; private set; }
        public ServiceFlags Flags { get; private set; }
        public uint InterfaceIndex { get; private set; }

        public LookupMessagePayload()
        {
        }

        public LookupMessagePayload(string hostName, ProtocolFlags protocolFlags, ServiceFlags flags, uint interfaceIndex)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (!protocolFlags.HasAnyFlag(ProtocolFlags.IPv4v6))
            {
                throw new ArgumentException(nameof(protocolFlags));
            }

            HostName = hostName;
            ProtocolFlags = protocolFlags;
            Flags = flags;
            InterfaceIndex = interfaceIndex;
        }

        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.NetworkToHostOrder((uint)Flags));
            writer.Write(ServiceMessage.HostToNetworkOrder(InterfaceIndex));
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)ProtocolFlags));
            writer.Write(ServiceMessage.GetMessageStringBytes(HostName));

            return ms.ToArray();
        }

        public override void Parse(byte[] bytes, ref int index)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (index < 0 || index > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

            base.Parse(bytes, ref index);
            Flags = (ServiceFlags)ServiceMessage.GetUInt32(bytes, ref index);
            InterfaceIndex = ServiceMessage.GetUInt32(bytes, ref index);
            ProtocolFlags = (ProtocolFlags)ServiceMessage.GetUInt32(bytes, ref index);
            HostName = ServiceMessage.GetString(bytes, ref index);
        }
    }
}
