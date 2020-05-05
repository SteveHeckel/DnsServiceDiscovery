using System;
using System.Collections.Generic;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Mittosoft.DnsServiceDiscovery.Records;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    internal class ResolveOperation : OperationBase
    {
        public ResolveOperation(string instanceName, string serviceType, string domain, uint interfaceIndex = 0, object context = null) :
            base(new ResolveMessage(instanceName, serviceType, domain, interfaceIndex), context)
        {
        }

        public event EventHandler<ResolveEventArgs> ResolveEvent;

        internal override void ProcessReply(CallbackMessage message, bool moreComing)
        {
            if (message is ResolveCallbackMessage rcm)
            {
                IEnumerable<DnssdTxtRecord> records = null;
                if (rcm.Payload.TxtRecord != null)
                {
                    var trb = new TxtRecordBuilder();
                    trb.Parse(rcm.Payload.TxtRecord, 0);
                    records = trb.TxtRecords;
                }

                ResolveEvent?.Invoke(Token, new ResolveEventArgs(rcm.Payload.FullName, rcm.Payload.HostName, rcm.Payload.Port, rcm.Payload.InterfaceIndex, records, moreComing));
            }
        }
    }
}
