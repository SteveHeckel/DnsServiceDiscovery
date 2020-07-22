using System;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    internal class RegisterOperation : OperationBase
    {
        public event EventHandler<RegistrationEventArgs> RegistrationEvent;

        public RegisterOperation(string instanceName, string serviceType, string domain, string host, ushort port, byte[] txtRecord = null, uint interfaceIndex = 0, object context = null) :
            base(new RegisterMessage(instanceName, serviceType, domain, host, port, txtRecord, ServiceFlags.None, interfaceIndex), context)
        {
        }

        internal override void ProcessReply(CallbackMessage message, bool moreComing)
        {
            if (message is RegisterCallbackMessage rcm)
            {
                var error = rcm.Payload.Error;
                var descriptor = new ServiceDescriptor(rcm.Payload.InstanceName, rcm.Payload.ServiceType, rcm.Payload.Domain, rcm.Payload.InterfaceIndex);
                var type = error == ServiceError.NoError ?
                    rcm.Payload.Flags.HasFlag(ServiceFlags.Add) ? RegistrationEventType.Added : RegistrationEventType.Removed :
                    RegistrationEventType.Error;
                RegistrationEvent?.Invoke(Token, new RegistrationEventArgs(type, descriptor, error, moreComing));
            }
        }
    }
}
