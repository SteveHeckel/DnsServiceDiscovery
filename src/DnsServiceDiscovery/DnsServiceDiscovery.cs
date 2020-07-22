using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Operations;

[assembly: InternalsVisibleTo("DnsServiceDiscovery.Tests")]
namespace Mittosoft.DnsServiceDiscovery
{
    public interface IDnsServiceDiscovery
    {
        event EventHandler<BrowseEventArgs> BrowseEvent;
        event EventHandler<RegistrationEventArgs> RegistrationEvent;
        event EventHandler<ResolveEventArgs> ResolveEvent;
        event EventHandler<LookupEventArgs> LookupEvent;
        Task<bool> ProbeServiceAsync(bool leaveConnected = true);
        Task<IOperationToken> BrowseAsync(string serviceType, string domain = "", uint interfaceIndex = 0, object context = null);
        Task<IOperationToken> RegisterAsync(string instanceName, string serviceType, string domain, string host, ushort port, byte[] txtRecord = null, uint interfaceIndex = 0, object context = null);
        Task<IOperationToken> ResolveAsync(ServiceDescriptor descriptor, object context = null);
        Task<IOperationToken> ResolveAsync(string instanceName, string serviceType, string domain, uint interfaceIndex = 0, object context = null);
        Task<IOperationToken> LookupAsync(string hostName, ProtocolFlags flags, bool withTimeout = false, uint interfaceIndex = 0, object context = null);
        Task CancelAllOperationsAsync();
    }

    public class DnsServiceDiscovery : IDnsServiceDiscovery
    {
        public event EventHandler<BrowseEventArgs> BrowseEvent;
        public event EventHandler<RegistrationEventArgs> RegistrationEvent;
        public event EventHandler<ResolveEventArgs> ResolveEvent;
        public event EventHandler<LookupEventArgs> LookupEvent;

        private ConnectionOperation _connection;

        internal static void SetServiceTransportProvider(IServiceTransportProvider provider)
        {
            OperationBase.TransportProvider = provider;
        }

        private async Task CheckConnectionAsync()
        {
            if (_connection == null)
            {
                _connection = new ConnectionOperation();
                _connection.Token.StateChanged += (s, e) =>
                {
                    if (e == OperationState.Faulted || e == OperationState.Canceled)
                        _connection = null;
                };

                await _connection.ExecuteAsync();
            }
        }

        public async Task<bool> ProbeServiceAsync(bool leaveConnected = true)
        {
            var result = false;

            try
            {
                await CheckConnectionAsync();

                result = true;

                if (!leaveConnected)
                    await _connection.CancelAsync();
            }
            catch (DnsServiceException e) when (e.ServiceError == ServiceError.ServiceNotRunning)
            {
            }

            return result;
        }

        public async Task<IOperationToken> BrowseAsync(string serviceType, string domain = "", uint interfaceIndex = 0, object context = null)
        {
            await CheckConnectionAsync();

            var op = new BrowseOperation(serviceType, domain, interfaceIndex, context);
            op.BrowseEvent += (s, e) => BrowseEvent?.Invoke(s, e);

            await _connection.AddAndExecuteSubordinate(op);

            return op.Token;
        }

        public async Task<IOperationToken> RegisterAsync(string instanceName, string serviceType, string domain, string host, ushort port, byte[] txtRecord, uint interfaceIndex = 0, object context = null)
        {
            await CheckConnectionAsync();

            var op = new RegisterOperation(instanceName, serviceType, domain, host, port, txtRecord, interfaceIndex, context);
            op.RegistrationEvent += (s, e) => RegistrationEvent?.Invoke(s, e);

            await _connection.AddAndExecuteSubordinate(op);

            return op.Token;
        }

        public Task<IOperationToken> ResolveAsync(ServiceDescriptor descriptor, object context = null)
        {
            return ResolveAsync(descriptor.InstanceName, descriptor.ServiceType, descriptor.Domain, descriptor.InterfaceIndex, context);
        }

        public async Task<IOperationToken> ResolveAsync(string instanceName, string serviceType, string domain, uint interfaceIndex = 0, object context = null)
        {
            await CheckConnectionAsync();

            var op = new ResolveOperation(instanceName, serviceType, domain, interfaceIndex, context);
            op.ResolveEvent += (s, e) => ResolveEvent?.Invoke(s, e);

            await _connection.AddAndExecuteSubordinate(op);

            return op.Token;
        }

        public async Task<IOperationToken> LookupAsync(string hostName, ProtocolFlags flags, bool withTimeout = false, uint interfaceIndex = 0, object context = null)
        {
            await CheckConnectionAsync();

            var op = new LookupOperation(hostName, flags, withTimeout, interfaceIndex, context);

            op.LookupEvent += (s, e) => LookupEvent?.Invoke(s, e);

            await _connection.AddAndExecuteSubordinate(op);

            return op.Token;
        }

        public async Task CancelAllOperationsAsync()
        {
            if (_connection != null)
            {
                await _connection.CancelAsync();
                _connection = null;
            }
        }
    }
}
