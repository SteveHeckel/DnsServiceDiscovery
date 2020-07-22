using System;
using System.IO;

namespace Mittosoft.DnsServiceDiscovery.Messages.Replies
{
    class BrowseCallbackMessage : CallbackMessage<BrowseCallbackMessagePayload>
    {
        public BrowseCallbackMessage(ServiceMessageHeader header) : this(header, new BrowseCallbackMessagePayload())
        {
        }

        public BrowseCallbackMessage(CallbackMessageBaseValues baseValues, string instanceName, string serviceType, string domain)
            : this(new ServiceMessageHeader(OperationCode.BrowseReply), new BrowseCallbackMessagePayload(baseValues, instanceName, serviceType, domain))
        {
        }

        public BrowseCallbackMessage(ServiceMessageHeader header, BrowseCallbackMessagePayload payload) : base(header, payload)
        {
            if (header.OperationCode != OperationCode.BrowseReply)
                throw new ArgumentException($"Header contains incorrect operation code [{header.OperationCode}] for this class");
        }
    }

    internal class BrowseCallbackMessagePayload : CallbackMessagePayload
    {
        public string InstanceName { get; private set; } = string.Empty;
        public string ServiceType { get; private set; } = string.Empty;
        public string Domain { get; private set; } = string.Empty;

        public BrowseCallbackMessagePayload()
        {
        }

        public BrowseCallbackMessagePayload(CallbackMessageBaseValues baseValues, string instanceName, string serviceType, string domain) : base(baseValues)
        {
            InstanceName = instanceName ?? string.Empty;
            ServiceType = serviceType ?? string.Empty;
            Domain = domain ?? string.Empty;
        }

        public override byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(base.GetBytes());
            writer.Write(ServiceMessage.GetMessageStringBytes(InstanceName));
            writer.Write(ServiceMessage.GetMessageStringBytes(ServiceType));
            writer.Write(ServiceMessage.GetMessageStringBytes(Domain));

            return ms.ToArray();
        }

        public override void Parse(byte[] bytes, ref int index)
        {
            base.Parse(bytes, ref index);
            InstanceName = ServiceMessage.GetString(bytes, ref index);
            ServiceType = ServiceMessage.GetString(bytes, ref index);
            Domain = ServiceMessage.GetString(bytes, ref index);
        }
    }
}
