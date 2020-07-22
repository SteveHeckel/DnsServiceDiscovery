using System;

namespace Mittosoft.DnsServiceDiscovery.Messages.Requests
{
    internal interface IRequestMessagePayload : IServiceMessagePayload
    {
        ushort? ErrorReturnPort { get; }
    }

    internal class RequestMessagePayload : IRequestMessagePayload
    {
        public bool IsSubordinateMessage { get; set; }
        public ushort? ErrorReturnPort { get; internal set; }

        public virtual byte[] GetBytes()
        {
            if (IsSubordinateMessage)
            {
                if (ErrorReturnPort.HasValue)
                    return BitConverter.GetBytes(ServiceMessage.HostToNetworkOrder(ErrorReturnPort.Value));
                else
                    throw new InvalidOperationException("RequestMessage is subordinate message but ErrorReturnPort is not set");
            }

            return new byte[0];
        }

        public virtual void Parse(byte[] bytes, ref int index)
        {
            if (IsSubordinateMessage)
                ErrorReturnPort = ServiceMessage.GetUInt16(bytes, ref index);
        }
    }

    internal class RequestMessage : ServiceMessage<RequestMessagePayload>
    {
        public new RequestMessagePayload Payload => base.Payload;

        public RequestMessage(OperationCode opCode) : this(new ServiceMessageHeader(opCode), null)
        {

        }

        public RequestMessage(ServiceMessageHeader header, RequestMessagePayload payload) : base(header, payload)
        {
        }
    }

    internal class RequestMessage<TPayload> : RequestMessage where TPayload : RequestMessagePayload
    {
        public new TPayload Payload => base.Payload as TPayload;

        public RequestMessage(ServiceMessageHeader header, TPayload payload) : base(header, payload)
        {
        }
    }
}
