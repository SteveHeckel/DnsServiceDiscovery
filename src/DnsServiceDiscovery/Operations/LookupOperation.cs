using System;
using System.Net;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    class LookupOperation : OperationBase
    {
        public LookupOperation(string hostName, ProtocolFlags flags, bool withTimeout, uint interfaceIndex = 0, object context = null) :
            base(new LookupMessage(hostName, flags, withTimeout, interfaceIndex), context)
        {
        }

        public event EventHandler<LookupEventArgs> LookupEvent;

        internal override void ProcessReply(CallbackMessage message, bool moreComing)
        {
            if (message is LookupCallbackMessage lcm)
            {
                // *** Comment from Bonjour/dnssd_clientstubb/handle_addrinfo_response() function ***
                // We only generate client callbacks for A and AAAA results (including NXDOMAIN results for
                // those types, if the client has requested those with the kDNSServiceFlagsReturnIntermediates).
                // Other result types, specifically CNAME referrals, are not communicated to the client, because
                // the DNSServiceGetAddrInfoReply interface doesn't have any meaningful way to communiate CNAME referrals.
                IPAddress ipa = null;
                if (lcm.Payload.RecordLength != 0 && (lcm.Payload.RecordType == ResourceRecordType.A || lcm.Payload.RecordType == ResourceRecordType.AAAA))
                {
                    var i = 0;
                    ipa = new IPAddress(ServiceMessage.GetSubArray(lcm.Payload.RecordData, ref i, lcm.Payload.RecordLength));
                }

                var eventType = LookupEventType.None;
                if (lcm.Payload.Error != ServiceError.NoError)
                    eventType = lcm.Payload.Error == ServiceError.Timeout ? LookupEventType.Timeout :
                                lcm.Payload.Error == ServiceError.NoSuchRecord ? LookupEventType.NoSuchRecord : LookupEventType.Error;
                else
                    eventType = lcm.Payload.Flags.HasFlag(ServiceFlags.Add) ? LookupEventType.Added : LookupEventType.Removed;

                LookupEvent?.Invoke(Token, new LookupEventArgs(eventType, lcm.Payload.HostName, ipa, lcm.Payload.TimeToLive, lcm.Payload.InterfaceIndex, moreComing));
            }
        }
    }
}
