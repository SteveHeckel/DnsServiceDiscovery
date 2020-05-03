using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;

namespace Mittosoft.DnsServiceDiscovery.Operations
{
    internal class ConnectionOperation : OperationBase
    {
        private readonly ConcurrentDictionary<ulong, OperationBase> _subordinates = new ConcurrentDictionary<ulong, OperationBase>();

        private ulong NextSubordinateID
        {
            get
            {
                var id = (ulong)1;
                var keys = _subordinates.Keys;
                while (keys.Contains(id)) id++;
                return id;
            }
        }

        public ConnectionOperation(object context = null) : base(new RequestMessage(OperationCode.ConnectionRequest), context)
        {
        }

        public async Task AddAndExecuteSubordinate(OperationBase subordinate)
        {
            if (State != OperationState.Executing)
                throw new InvalidOperationException($"ConnectionOperation is in the {State} state");

            AddSubordinate(subordinate);
            // subordinate will remove itself if it fails to execute
            await subordinate.ExecuteAsync();
        }

        internal void RemoveSubordinate(OperationBase subordinate)
        {
            _subordinates.TryRemove(subordinate.SubordinateID, out subordinate);
        }

        internal void AddSubordinate(OperationBase subordinate)
        {
            var id = NextSubordinateID;
            if (!_subordinates.TryAdd(id, subordinate))
                throw new DnsServiceException(ServiceError.BadParam);
            subordinate.SubordinateID = id;
            subordinate.Connection = this;
        }

        internal bool IsSubordinate(OperationBase operation)
        {
            return _subordinates.ContainsKey(operation.SubordinateID);
        }

        protected override void OnPrimaryCanceled()
        {
            foreach (var subordinate in _subordinates.Values)
            {
                subordinate.OnCanceled();
            }
            _subordinates.Clear();
        }

        protected override void ConnectionClosed(ConnectionClosedReason reason)
        {
            base.ConnectionClosed(reason);
            foreach (var subordinate in _subordinates.Values)
                subordinate.State = State;
        }

        internal override void ProcessReply(CallbackMessage message, bool moreComing)
        {
            if (message.Header.OperationCode != OperationCode.RegisterRecordReply)
            {
                if (_subordinates.TryGetValue(message.Header.SubordinateID, out var operation))
                    operation?.ProcessReply(message, moreComing);
            }
            else // Not handling register record reply yet
            {
                throw new NotImplementedException();
            }
        }
    }
}
