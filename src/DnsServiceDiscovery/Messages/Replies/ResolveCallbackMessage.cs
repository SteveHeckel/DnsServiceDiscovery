using System;
using System.IO;
using Ardalis.GuardClauses;

namespace Mittosoft.DnsServiceDiscovery.Messages.Replies
{
    class ResolveCallbackMessage : CallbackMessage<ResolveCallbackMessagePayload>
    {
        public ResolveCallbackMessage(ServiceMessageHeader header) : this(header, new ResolveCallbackMessagePayload())
        {
        }

        public ResolveCallbackMessage(string hostName, string fullName, ushort port, byte[] txtRecord = null)
            : this(CallbackMessageBaseValues.Default, hostName, fullName, port, txtRecord)
        {
        }

        public ResolveCallbackMessage(CallbackMessageBaseValues baseValues, string hostName, string fullName, ushort port, byte[] txtRecord = null)
            : this(new ServiceMessageHeader(OperationCode.ResolveReply),
                new ResolveCallbackMessagePayload(baseValues, hostName, fullName, port, txtRecord))
        {
        }

        public ResolveCallbackMessage(ServiceMessageHeader header, ResolveCallbackMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.ResolveReply)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class ResolveCallbackMessagePayload : CallbackMessagePayload
    {
        public string FullName { get; private set; } = string.Empty;
        public string HostName { get; private set; } = string.Empty;
        public ushort Port { get; private set; }
        public byte[] TxtRecord { get; private set; }


        public ResolveCallbackMessagePayload()
        {
        }

        public ResolveCallbackMessagePayload(CallbackMessageBaseValues baseValues, string hostName, string fullName, ushort port, byte[] txtRecord) : base(baseValues)
        {
            HostName = hostName ?? string.Empty;
            FullName = fullName ?? string.Empty;
            Port = port;
            TxtRecord = txtRecord;
        }

        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.GetMessageStringBytes(FullName));
            writer.Write(ServiceMessage.GetMessageStringBytes(HostName));
            writer.Write(ServiceMessage.HostToNetworkOrder(Port));
            writer.Write(ServiceMessage.HostToNetworkOrder((ushort)(TxtRecord?.Length ?? 0)));
            if (TxtRecord != null)
                writer.Write(TxtRecord);

            return ms.ToArray();
        }

        public override void Parse(byte[] bytes, ref int index)
        {
            base.Parse(bytes, ref index);
            FullName = ServiceMessage.GetString(bytes, ref index);
            HostName = ServiceMessage.GetString(bytes, ref index);
            Port = ServiceMessage.GetUInt16(bytes, ref index);
            var txtRecordLength = ServiceMessage.GetUInt16(bytes, ref index);
            if (txtRecordLength != 0)
                TxtRecord = ServiceMessage.GetSubArray(bytes, ref index, txtRecordLength);
        }

    }
}
