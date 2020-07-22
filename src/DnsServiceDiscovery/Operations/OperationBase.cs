using System;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    public enum OperationState
    {
        New,
        Executing,
        Canceled,
        Faulted,
    }

    internal abstract class OperationBase
    {
        internal static IServiceTransportProvider TransportProvider { get; set; } = new ServiceTransportProvider();
        internal RequestMessage Message { get; }
        private OperationState _state;
        protected internal OperationState State
        {
            get => _state;
            set => Token.State = _state = value;
        }
        public event EventHandler OperationCanceled;
        internal OperationToken Token { get; }
        internal ulong SubordinateID
        {
            get => Message.Header.SubordinateID;
            set => Message.SetSubordinateID(value);
        }
        private ConnectionOperation _connection;
        protected internal ConnectionOperation Connection
        {
            get => _connection;
            set
            {
                if (value != null && State != OperationState.New)
                    throw new InvalidOperationException($"The {nameof(Connection)} property can only be set when the operation state is {nameof(OperationState.New)}");
                if (value != null && !value.IsSubordinate(this))
                    throw new InvalidOperationException("Instance must be added to Primary as a subordinate prior to setting");
                if (value == null)
                    _connection?.RemoveSubordinate(this);

                _connection = value;
            }
        }
        protected bool IsPrimary => _connection == null;
        private IServiceTransport _transport;

        protected IServiceTransport Transport
        {
            get => IsPrimary ? _transport : _connection.Transport;
            set
            {
                if (!IsPrimary)
                    throw new InvalidOperationException($"The {nameof(Transport)} property cannot be set on a non-primary operation");
                if (_transport != null)
                    throw new IndexOutOfRangeException($"The {nameof(Transport)} property can only be set once");

                _transport = value;
            }
        }

        protected OperationBase(RequestMessage message, object context = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Token = new OperationToken(this, context);
        }

        private readonly SemaphoreLocker _locker = new SemaphoreLocker();

        internal async Task ExecuteAsync()
        {
            await _locker.LockAsync(async () =>
            {
                if (State != OperationState.New)
                    throw new InvalidOperationException($"Operation state must be {nameof(OperationState.New)}");

                try
                {
                    if (IsPrimary)
                    {
                        Transport = await TransportProvider.GetServiceTransport();
                        await Transport.StartAsync(Message, ProcessReply, ConnectionClosed);
                    }
                    else
                        await Transport.SendMessageAsync(Message, true);
                }
                catch (Exception)
                {
                    State = OperationState.Faulted;
                    // Setting to null removes it as subordinate
                    Connection = null;
                    throw;
                }

                State = OperationState.Executing;
            });
        }

        // This is the equivalent of DNSServiceRefDeallocate() in the Bojour dnssd API
        internal async Task CancelAsync()
        {
            await _locker.LockAsync(async () =>
            {
                if (State != OperationState.Executing)
                    return;

                if (IsPrimary)
                {
                    await Transport.StopAsync();
                    OnPrimaryCanceled();
                }
                else
                {
                    var cancelMessage = new ServiceMessage(OperationCode.CancelRequest);
                    cancelMessage.SetSubordinateID(SubordinateID);
                    await Transport.PostMessageAsync(cancelMessage);
                    Connection = null;
                }

                OnCanceled();
            });
        }

        protected virtual void OnPrimaryCanceled() { }

        internal void OnCanceled()
        {
            if (State == OperationState.Executing)
                State = OperationState.Canceled;

            OperationCanceled?.Invoke(Token, null);
        }

        protected virtual void ConnectionClosed(ConnectionClosedReason reason)
        {
            switch (reason)
            {
                case ConnectionClosedReason.Canceled:
                    State = OperationState.Canceled;
                    break;
                default:
                    State = OperationState.Faulted;
                    break;
            }
        }

        internal abstract void ProcessReply(CallbackMessage message, bool moreComing);
    }
}
