using System;
using System.IO;
using Ardalis.GuardClauses;

namespace Mittosoft.DnsServiceDiscovery.Messages.Requests
{
    internal class BrowseMessage : RequestMessage<BrowseMessagePayload>
    {
        public BrowseMessage(ServiceMessageHeader header) : this(header, new BrowseMessagePayload())
        {
        }

        public BrowseMessage(string serviceType, string domain, uint interfaceIndex = 0)
            : this(new ServiceMessageHeader(OperationCode.BrowseRequest), new BrowseMessagePayload(serviceType, domain, ServiceFlags.None, interfaceIndex))
        {
        }

        public BrowseMessage(ServiceMessageHeader header, BrowseMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.BrowseRequest)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class BrowseMessagePayload : RequestMessagePayload
    {
        public ServiceFlags Flags { get; private set; }
        public uint InterfaceIndex { get; private set; }
        public string ServiceType { get; private set; } = string.Empty;
        public string Domain { get; private set; } = string.Empty;

        public BrowseMessagePayload()
        {
        }

        public BrowseMessagePayload(string serviceType, string domain, ServiceFlags flags, uint interfaceIndex)
        {
            Guard.Against.NullOrEmpty(serviceType, nameof(serviceType));

            Flags = flags;
            InterfaceIndex = interfaceIndex;
            ServiceType = serviceType;
            Domain = domain ?? string.Empty;
        }


        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)Flags));
            writer.Write(ServiceMessage.HostToNetworkOrder(InterfaceIndex));
            writer.Write(ServiceMessage.GetMessageStringBytes(ServiceType));
            writer.Write(ServiceMessage.GetMessageStringBytes(Domain));

            return ms.ToArray();
        }

        public override void Parse(byte[] bytes, ref int index)
        {
            Guard.Against.Null(bytes, nameof(bytes));
            Guard.Against.OutOfRange(index, nameof(index), 0, bytes.Length);

            base.Parse(bytes, ref index);
            Flags = (ServiceFlags)ServiceMessage.GetUInt32(bytes, ref index);
            InterfaceIndex = ServiceMessage.GetUInt32(bytes, ref index);
            ServiceType = ServiceMessage.GetString(bytes, ref index);
            Domain = ServiceMessage.GetString(bytes, ref index);

        }
    }
}
