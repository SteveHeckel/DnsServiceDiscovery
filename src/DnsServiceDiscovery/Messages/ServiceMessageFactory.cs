using System;
using System.Collections.Generic;
using Mittosoft.DnsServiceDiscovery.Helpers;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Messages
{
    internal static class ServiceMessageFactory
    {
        private static readonly Dictionary<OperationCode, Func<ServiceMessageHeader, ServiceMessage>> FactoryDictionary =
            new Dictionary<OperationCode, Func<ServiceMessageHeader, ServiceMessage>>
            {
                {OperationCode.BrowseRequest, (h) => new BrowseMessage(h) },
                {OperationCode.AddressInfoRequest, (h) => new LookupMessage(h) },
                {OperationCode.RegisterServiceRequest, (h) => new RegisterMessage(h) },
                {OperationCode.ResolveRequest, (h) => new ResolveMessage(h) },
                {OperationCode.BrowseReply, (h) => new BrowseCallbackMessage(h) },
                {OperationCode.AddressInfoReply, (h) => new LookupCallbackMessage(h) },
                {OperationCode.RegisterServiceReply, (h) => new RegisterCallbackMessage(h) },
                {OperationCode.ResolveReply, (h) => new ResolveCallbackMessage(h) }
            };

        public static CallbackMessage GetCallbackMessage(ServiceMessageHeader header, byte[] bytes, int index)
        {
            return GetServiceMessage<CallbackMessage>(header, bytes, index) ?? new CallbackMessage(header, null);
        }

        public static RequestMessage GetRequestMessage(ServiceMessageHeader header, byte[] bytes, int index)
        {
            return GetServiceMessage<RequestMessage>(header, bytes, index) ?? new RequestMessage(header, null);
        }

        public static TMessage GetServiceMessage<TMessage>(ServiceMessageHeader header, byte[] bytes, int index) where TMessage : ServiceMessage
        {
            TMessage sm = null;

            if (FactoryDictionary.ContainsKey(header.OperationCode))
                sm = FactoryDictionary[header.OperationCode].Invoke(header) as TMessage;

            sm?.Payload?.Parse(bytes, ref index);

            return sm;
        }

        public static ServiceMessage GetServiceMessage(byte[] bytes, int index)
        {
            var header = ServiceMessageHeader.Parse(bytes, ref index);
            var sm = GetServiceMessage<ServiceMessage>(header, bytes, index) ??
                    new ServiceMessage(header, new ServiceMessagePayload(bytes.SubArray(index, bytes.Length - index)));

            return sm;
        }
    }
}
