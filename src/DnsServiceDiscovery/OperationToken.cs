using System;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Operations;

namespace Mittosoft.DnsServiceDiscovery
{
    public interface IOperationToken
    {
        OperationState State { get; }
        object Context { get; }

        event EventHandler<OperationState> StateChanged;

        Task CancelAsync();
    }

    internal class OperationToken : IOperationToken
    {
        private OperationState _state;
        public OperationState State
        {
            get => _state;
            internal set
            {
                if (_state != value)
                {
                    _state = value;
                    _stateChanged?.Invoke(this, _state);
                }
            }
        }

        private readonly OperationBase _descriptor;

        public object Context { get; }

        private EventHandler<OperationState> _stateChanged;
        public event EventHandler<OperationState> StateChanged
        {
            add
            {
                _stateChanged += value;
                // Let the caller know that the state may have changed since the operation was constructed/started.
                if (State != OperationState.New)
                    value.Invoke(this, State);
            }
            remove
            {
                // ReSharper disable once DelegateSubtraction
                if (_stateChanged != null) _stateChanged -= value;
            }
        }

        internal OperationToken(OperationBase descriptor, object context = null)
        {
            _descriptor = descriptor;
            Context = context;
        }

        public async Task CancelAsync()
        {
            await _descriptor.CancelAsync();
        }
    }
}
