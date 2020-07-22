using System;
using System.IO;

namespace Mittosoft.DnsServiceDiscovery.Messages.Replies
{
    class LookupCallbackMessage : CallbackMessage<LookupCallbackMessagePayload>
    {
        public LookupCallbackMessage(ServiceMessageHeader header) : this(header, new LookupCallbackMessagePayload())
        {
        }

        public LookupCallbackMessage(CallbackMessageBaseValues baseValues, string hostName, ResourceRecordType rrType, ushort rrClass, byte[] rrData, uint ttl)
            : this(new ServiceMessageHeader(OperationCode.AddressInfoReply),
                new LookupCallbackMessagePayload(baseValues, hostName, rrType, rrClass, rrData, ttl))
        {
        }

        public LookupCallbackMessage(ServiceMessageHeader header, LookupCallbackMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.AddressInfoReply)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class LookupCallbackMessagePayload : CallbackMessagePayload
    {
        public string HostName { get; private set; } = string.Empty;
        public ResourceRecordType RecordType { get; private set; }
        public ushort RecordClass { get; private set; }
        public ushort RecordLength => (ushort)(RecordData?.Length ?? 0);
        public byte[] RecordData { get; private set; }
        public uint TimeToLive { get; private set; }

        public LookupCallbackMessagePayload()
        {
        }

        public LookupCallbackMessagePayload(CallbackMessageBaseValues baseValues, string hostName, ResourceRecordType rrType,
            ushort rrClass, byte[] rrData, uint ttl) : base(baseValues)
        {
            HostName = hostName ?? string.Empty;
            RecordType = rrType;
            RecordClass = rrClass;
            RecordData = rrData;
            TimeToLive = ttl;
        }

        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.GetMessageStringBytes(HostName));
            writer.Write(ServiceMessage.HostToNetworkOrder((ushort)RecordType));
            writer.Write(ServiceMessage.HostToNetworkOrder(RecordClass));
            writer.Write(ServiceMessage.HostToNetworkOrder(RecordLength));
            if (RecordData != null)
                writer.Write(RecordData);
            writer.Write(ServiceMessage.HostToNetworkOrder(TimeToLive));

            return ms.ToArray();
        }

        public override void Parse(byte[] bytes, ref int index)
        {
            base.Parse(bytes, ref index);
            HostName = ServiceMessage.GetString(bytes, ref index);
            RecordType = (ResourceRecordType)ServiceMessage.GetUInt16(bytes, ref index);
            RecordClass = ServiceMessage.GetUInt16(bytes, ref index);
            var recordLength = ServiceMessage.GetUInt16(bytes, ref index);
            if (recordLength != 0)
                RecordData = ServiceMessage.GetSubArray(bytes, ref index, recordLength);
            TimeToLive = ServiceMessage.GetUInt32(bytes, ref index);
        }
    }
}
