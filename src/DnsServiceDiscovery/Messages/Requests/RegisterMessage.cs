using System;
using System.IO;
using System.Linq.Expressions;
using Ardalis.GuardClauses;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;

namespace Mittosoft.DnsServiceDiscovery.Messages.Requests
{
    class RegisterMessage : RequestMessage<RegisterMessagePayload>
    {
        public RegisterMessage(ServiceMessageHeader header) : this(header, new RegisterMessagePayload())
        {
        }

        public RegisterMessage(string instanceName, string serviceType, string domain, string host, ushort port, byte[] txtRecord = null, ServiceFlags flags = ServiceFlags.None, uint interfaceIndex = 0)
            : this(new ServiceMessageHeader(OperationCode.RegisterServiceRequest), new RegisterMessagePayload(instanceName, serviceType, domain, host, port,
                txtRecord, flags, interfaceIndex))
        {
        }

        public RegisterMessage(ServiceMessageHeader header, RegisterMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.RegisterServiceRequest)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class RegisterMessagePayload : RequestMessagePayload
    {
        public string InstanceName { get; private set; } = string.Empty;
        public string ServiceType { get; private set; } = string.Empty;
        public string Domain { get; private set; } = string.Empty;
        public string HostName { get; private set; } = string.Empty;
        public ushort Port { get; private set; }
        public byte[] TxtRecord { get; private set; }
        public ServiceFlags Flags { get; private set; }
        public uint InterfaceIndex { get; private set; }

        public RegisterMessagePayload()
        {
        }

        public RegisterMessagePayload(string instanceName, string serviceType, string domain, string hostName, ushort port, byte[] txtRecord, ServiceFlags flags, uint interfaceIndex)
        {
            Guard.Against.NullOrEmpty(serviceType, nameof(serviceType));
            
            InstanceName = instanceName ?? string.Empty;
            ServiceType = serviceType;
            Domain = domain ?? string.Empty;
            HostName = hostName ?? string.Empty;
            Port = port;
            TxtRecord = txtRecord;
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
            writer.Write(ServiceMessage.GetMessageStringBytes(HostName));
            writer.Write(ServiceMessage.HostToNetworkOrder(Port));
            writer.Write(ServiceMessage.HostToNetworkOrder((ushort)(TxtRecord?.Length ?? 0)));
            if (TxtRecord != null)
                writer.Write(TxtRecord);

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
            HostName = ServiceMessage.GetString(bytes, ref index);
            Port = ServiceMessage.GetUInt16(bytes, ref index);
            var txtRecLength = ServiceMessage.GetUInt16(bytes, ref index);
            if (txtRecLength != 0)
                TxtRecord = ServiceMessage.GetSubArray(bytes, ref index, txtRecLength);
        }
    }
}
