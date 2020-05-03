using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mittosoft.DnsServiceDiscovery.Messages
{
    internal interface IServiceMessageHeader
    {
        uint Version { get; }
        uint DataLength { get; }
        ServiceFlags Flags { get; }
        OperationCode OperationCode { get; }
        ulong SubordinateID { get; }
        uint RegIndex { get; }
    }

    internal sealed class ServiceMessageHeader : IByteStreamSerializable, IServiceMessageHeader
    {
        public uint Version { get; private set; }
        public uint DataLength { get; internal set; }
        public ServiceFlags Flags { get; private set; }
        public OperationCode OperationCode { get; private set; }
        public ulong SubordinateID { get; internal set; }
        // Comment from Bonjour source - dnssd_ipc.h line 177, definition of ipc_msg_header struct
        // identifier for a record registered via DNSServiceRegisterRecord() on a
        // socket connected by DNSServiceCreateConnection().  Must be unique in the scope of the connection, such that and
        // index/socket pair uniquely identifies a record.  (Used to select records for removal by DNSServiceRemoveRecord())
        public uint RegIndex { get; private set; }
        public const int CurrentVersion = 1;
        public const int Length = (sizeof(uint) * 5) + sizeof(ulong);

        public ServiceMessageHeader()
        {
        }

        public ServiceMessageHeader(OperationCode opCode) : this(CurrentVersion, 0, ServiceFlags.None, opCode)
        {
        }

        public ServiceMessageHeader(uint version, uint dataLength, ServiceFlags flags, OperationCode opCode, ulong subordinateID = 0, uint regIndex = 0)
        {
            if (opCode == OperationCode.None)
                throw new ArgumentException($"Parameter {nameof(opCode)} cannont be {nameof(OperationCode.None)}");

            Version = version;
            DataLength = dataLength;
            Flags = flags;
            OperationCode = opCode;
            SubordinateID = subordinateID;
            RegIndex = regIndex;
        }

        public byte[] GetBytes()
        {
            var ms = new MemoryStream(Length);
            var writer = new BinaryWriter(ms);

            writer.Write(ServiceMessage.HostToNetworkOrder(Version));
            writer.Write(ServiceMessage.HostToNetworkOrder(DataLength));
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)Flags));
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)OperationCode));
            writer.Write(ServiceMessage.HostToNetworkOrder(SubordinateID));
            writer.Write(ServiceMessage.HostToNetworkOrder(RegIndex));

            return ms.ToArray();
        }

        public static ServiceMessageHeader Parse(byte[] bytes, int index)
        {
            return Parse(bytes, ref index);
        }

        public static ServiceMessageHeader Parse(byte[] bytes, ref int index)
        {
            var header = new ServiceMessageHeader();

            ((IByteStreamSerializable)header).Parse(bytes, ref index);

            return header;
        }

        void IByteStreamSerializable.Parse(byte[] bytes, ref int index)
        {
            if (bytes.Length - index < Length)
                throw new IndexOutOfRangeException($"An index value of {index} would cause an IndexOutOfRangeException while parsing the input data with length {bytes.Length}");

            var ms = new MemoryStream(bytes, index, bytes.Length - index);
            var reader = new BinaryReader(ms);

            Version = ServiceMessage.NetworkToHostOrder(reader.ReadUInt32());
            DataLength = ServiceMessage.NetworkToHostOrder(reader.ReadUInt32());
            Flags = (ServiceFlags)ServiceMessage.NetworkToHostOrder(reader.ReadUInt32());
            OperationCode = (OperationCode)ServiceMessage.NetworkToHostOrder(reader.ReadUInt32());
            SubordinateID = ServiceMessage.NetworkToHostOrder(reader.ReadUInt64());
            RegIndex = ServiceMessage.NetworkToHostOrder(reader.ReadUInt32());

            index = (int)ms.Position;
        }
    }
}
