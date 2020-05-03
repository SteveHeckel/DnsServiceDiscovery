using System.IO;
using System.Linq.Expressions;

namespace Mittosoft.DnsServiceDiscovery.Messages.Replies
{
    internal interface ICallbackMessagePayload : IServiceMessagePayload
    {
        ServiceFlags Flags { get; }
        uint InterfaceIndex { get; }
        ServiceError Error { get; }
    }

    internal class CallbackMessagePayload : ICallbackMessagePayload
    {
        public bool IsSubordinateMessage { get; set; }
        
        public ServiceFlags Flags { get; private set; }
        public uint InterfaceIndex { get; private set; }
        public ServiceError Error { get; private set; }
        public (ServiceFlags flags, uint InterfaceIndex, ServiceError error) BaseValues
        {
            get => (Flags, InterfaceIndex, Error);
            set => (Flags, InterfaceIndex, Error) = value;
        }

        public CallbackMessagePayload()
        {
        }

        public CallbackMessagePayload(CallbackMessageBaseValues baseValues)
        {
            BaseValues = baseValues;
        }

        public virtual byte[] GetBytes()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            writer.Write(ServiceMessage.HostToNetworkOrder((uint)Flags));
            writer.Write(ServiceMessage.HostToNetworkOrder(InterfaceIndex));
            writer.Write(ServiceMessage.HostToNetworkOrder((uint)Error));

            return ms.ToArray();
        }

        public virtual void Parse(byte[] bytes, ref int index)
        {
            Flags = (ServiceFlags)ServiceMessage.GetUInt32(bytes, ref index);
            InterfaceIndex = ServiceMessage.GetUInt32(bytes, ref index);
            Error = (ServiceError)ServiceMessage.GetUInt32(bytes, ref index);
        }
    }

    internal class CallbackMessage : ServiceMessage<CallbackMessagePayload>
    {
        public new CallbackMessagePayload Payload => base.Payload;

        public CallbackMessage(ServiceMessageHeader header, CallbackMessagePayload payload) : base(header, payload)
        {
        }
    }

    internal class CallbackMessage<TPayload> : CallbackMessage where TPayload : CallbackMessagePayload
    {
        public new TPayload Payload => base.Payload as TPayload;

        public CallbackMessage(ServiceMessageHeader header, TPayload payload) : base(header, payload)
        {
        }
    }

    internal readonly struct CallbackMessageBaseValues
    {
        public readonly ServiceFlags Flags;
        public readonly uint InterfaceIndex;
        public readonly ServiceError Error;

        public static CallbackMessageBaseValues Default = (ServiceFlags.None, 0, ServiceError.NoError);

        public CallbackMessageBaseValues(ServiceFlags flags, uint interfaceIndex, ServiceError error)
        {
            Flags = flags;
            InterfaceIndex = interfaceIndex;
            Error = error;
        }

        public void Deconstruct(out ServiceFlags flags, out uint interfaceIndex, out ServiceError error)
        {
            flags = Flags;
            interfaceIndex = InterfaceIndex;
            error = Error;
        }

        public static implicit operator CallbackMessageBaseValues(
            (ServiceFlags flags, uint InterfaceIndex, ServiceError error) tuple)
        {
            var (flags, interfaceIndex, error) = tuple;
            return new CallbackMessageBaseValues(flags, interfaceIndex, error);
        }

        public static implicit operator CallbackMessageBaseValues(ServiceFlags flags)
        {
            return new CallbackMessageBaseValues(flags, 0, ServiceError.NoError);
        }

        public static implicit operator (ServiceFlags flags, uint InterfaceIndex, ServiceError error)(
            CallbackMessageBaseValues baseValues)
        {
            var (flags, interfaceIndex, error) = baseValues;
            return (flags, interfaceIndex, error);
        }
    }
}
