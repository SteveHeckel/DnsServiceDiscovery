using System;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    class BrowseOperation : OperationBase
    {
        public event EventHandler<BrowseEventArgs> BrowseEvent;
        
        public BrowseOperation(string serviceType, string domain = null, uint interfaceIndex = 0, object context = null) :
            base(new BrowseMessage(serviceType, domain, interfaceIndex), context)
        {
        }

        internal override void ProcessReply(CallbackMessage message, bool moreComing)
        {
            if (message is BrowseCallbackMessage brm)
                BrowseEvent?.Invoke(Token, new BrowseEventArgs(brm.Payload.Flags.HasFlag(ServiceFlags.Add) ? BrowseEventType.Added : BrowseEventType.Removed,
                    new ServiceDescriptor(brm.Payload.InstanceName, brm.Payload.ServiceType, brm.Payload.Domain, brm.Payload.InterfaceIndex), moreComing));
        }
    }
}
