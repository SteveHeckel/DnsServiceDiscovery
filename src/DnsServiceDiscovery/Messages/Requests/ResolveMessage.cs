using System;
using System.IO;
using Ardalis.GuardClauses;

namespace Mittosoft.DnsServiceDiscovery.Messages.Requests
{
    internal class ResolveMessage : RequestMessage<ResolveMessagePayload>
    {
        public ResolveMessage(ServiceMessageHeader header) : this(header, new ResolveMessagePayload())
        {
        }

        public ResolveMessage(string instanceName, string serviceType, string domain, uint interfaceIndex = 0)
            : this(new ServiceMessageHeader(OperationCode.ResolveRequest),
                new ResolveMessagePayload(instanceName, serviceType, domain, ServiceFlags.None, interfaceIndex))
        {
        }

        public ResolveMessage(ServiceMessageHeader header, ResolveMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.ResolveRequest)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class ResolveMessagePayload : RequestMessagePayload
    {
        public string InstanceName { get; private set; } = string.Empty;
        public string ServiceType { get; private set; } = string.Empty;
        public string Domain { get; private set; } = string.Empty;
        public ServiceFlags Flags { get; private set; }
        public uint InterfaceIndex { get; private set; }

        public ResolveMessagePayload()
        {
        }

        public ResolveMessagePayload(string instanceName, string serviceType, string domain, ServiceFlags flags, uint interfaceIndex)
        {
            Guard.Against.NullOrEmpty(instanceName, nameof(instanceName));
            Guard.Against.NullOrEmpty(serviceType, nameof(serviceType));
            Guard.Against.NullOrEmpty(domain, nameof(domain));
            
            InstanceName = instanceName;
            ServiceType = serviceType;
            Domain = domain;
            Flags = flags;
            InterfaceIndex = interfaceIndex;
        }

        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)Flags));
            writer.Write(ServiceMessage.HostToNetworkOrder(InterfaceIndex));
            writer.Write(ServiceMessage.GetMessageStringBytes(InstanceName));
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
            InstanceName = ServiceMessage.GetString(bytes, ref index);
            ServiceType = ServiceMessage.GetString(bytes, ref index);
            Domain = ServiceMessage.GetString(bytes, ref index);
        }
    }
}
