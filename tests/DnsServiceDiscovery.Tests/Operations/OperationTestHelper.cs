using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using DnsServiceDiscovery.Tests.Communication;
using FluentAssertions;
using Nito.AsyncEx;
using Mittosoft.DnsServiceDiscovery;
using Mittosoft.DnsServiceDiscovery.Communication;
using Mittosoft.DnsServiceDiscovery.Messages.Replies;
using Mittosoft.DnsServiceDiscovery.Messages.Requests;
using Mittosoft.DnsServiceDiscovery.Operations;
using Mittosoft.DnsServiceDiscovery.Records;
using ResolveEventArgs = Mittosoft.DnsServiceDiscovery.ResolveEventArgs;

namespace DnsServiceDiscovery.Tests.Operations
{
    public class OperationTestHelper
    {
        private readonly TestServiceTransportProvider _provider = new TestServiceTransportProvider();
        internal Func<ConnectionClosedReason, bool> AlternateConnectorClosedHandler { get; set; }
        internal Func<RequestMessage, Task> AlternateProcessRequestMessageHandler { get; set; }
        private bool _connectorClosed = false;

        internal async Task<TOperation> PerformOperationExecuteTestAsPrimary<TOperation>(TOperation operation, bool cancelAndCheckStateOnExit = true)
            where TOperation : OperationBase
        {
            Guard.Against.Null(operation, nameof(operation));

            OperationBase.TransportProvider = _provider;

            _provider.Connector.Start(ProcessRequestMessage, ServiceTransportConnectorClosed);
            await operation.ExecuteAsync();
            operation.State.Should().Be(OperationState.Executing);
            ThrowIfConnectorClosed();
            if (cancelAndCheckStateOnExit)
            {
                await operation.CancelAsync();
                operation.State.Should().Be(OperationState.Canceled);
            }

            return operation;
        }

        internal async Task PerformOperationExecuteTestAsSubordinate(OperationBase operation, bool cancelAndCheckStateOnExit = true)
        {
            var connOp = await PerformOperationExecuteTestAsPrimary(new ConnectionOperation(), false);
            await connOp.AddAndExecuteSubordinate(operation);
            operation.State.Should().Be(OperationState.Executing);
            ThrowIfConnectorClosed();
            if (cancelAndCheckStateOnExit)
            {
                await operation.CancelAsync();
                operation.State.Should().Be(OperationState.Canceled);
                await connOp.CancelAsync();
                connOp.State.Should().Be(OperationState.Canceled);
            }
        }

        private CallbackMessage _callbackMessageUponRequest;
        private readonly AsyncManualResetEvent _manualResetEvent = new AsyncManualResetEvent();

        internal async Task PerformOperationExecuteTestAsSubordinateWithCallback(OperationBase operation, CallbackMessage cbMessage, bool cancelAndCheckStateOnExit = true)
        {
            _manualResetEvent.Reset();
            _callbackMessageUponRequest = cbMessage;
            HookOperationEvent(operation);
            var connOp = await PerformOperationExecuteTestAsPrimary(new ConnectionOperation(), false);
            await connOp.AddAndExecuteSubordinate(operation);
            operation.State.Should().Be(OperationState.Executing);
            if (await WaitForCallback())
                GetComparisonValuesFromMessage(cbMessage).Equals(CallbackValues).Should().Be(true);
            else
                throw new Exception("Timeout waiting for callback message");
            ThrowIfConnectorClosed();
            if (cancelAndCheckStateOnExit)
            {
                await operation.CancelAsync();
                operation.State.Should().Be(OperationState.Canceled);
                await connOp.CancelAsync();
                connOp.State.Should().Be(OperationState.Canceled);
            }
        }

        private async Task<bool> WaitForCallback()
        {
            var result = false;
            try
            {
                var cts = new CancellationTokenSource();
                var timerTask = Task.Delay(2000, cts.Token);
                var mreTask = _manualResetEvent.WaitAsync(cts.Token);
                var cTask = await Task.WhenAny(timerTask, mreTask);
                result = cTask == mreTask;
                cts.Cancel();
            }
            catch (OperationCanceledException)
            {
            }

            return result;
        }

        private IStructuralEquatable GetComparisonValuesFromMessage(CallbackMessage message)
        {
            IStructuralEquatable result = null;

            switch (message)
            {
                case BrowseCallbackMessage msg:
                    result = (OperationCallbackType.Browse, msg.Payload.Flags.HasFlag(ServiceFlags.Add) ? BrowseEventType.Added : BrowseEventType.Removed,
                                msg.Payload.InstanceName, msg.Payload.ServiceType, msg.Payload.Domain, msg.Payload.InterfaceIndex);
                    break;
                case RegisterCallbackMessage msg:
                    result = (OperationCallbackType.Register,
                        msg.Payload.Flags.HasFlag(ServiceFlags.Add)
                            ? RegistrationEventType.Added
                            : RegistrationEventType.Removed,
                        msg.Payload.InstanceName, msg.Payload.ServiceType, msg.Payload.Domain, msg.Payload.InterfaceIndex);
                    break;
                case ResolveCallbackMessage msg:
                    result = (OperationCallbackType.Resolve, msg.Payload.FullName, msg.Payload.HostName,
                        msg.Payload.Port, msg.Payload.TxtRecord, msg.Payload.InterfaceIndex);
                    break;
                case LookupCallbackMessage msg:
                    result = (OperationCallbackType.Lookup,
                        msg.Payload.Flags.HasFlag(ServiceFlags.Add) ? LookupEventType.Added : LookupEventType.Removed,
                        msg.Payload.HostName, new IPAddress(msg.Payload.RecordData), msg.Payload.TimeToLive,
                        msg.Payload.InterfaceIndex);
                    break;
            }

            return result;
        }

        private readonly string[] _eventNames =
        {
            nameof(BrowseOperation.BrowseEvent),
            nameof(RegisterOperation.RegistrationEvent),
            nameof(ResolveOperation.ResolveEvent),
            nameof(LookupOperation.LookupEvent)
        };

        internal void HookOperationEvent(OperationBase operation)
        {
            var opEvents = operation.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public);

            foreach (var opEvent in opEvents)
            {
                if (_eventNames.Contains(opEvent.Name))
                {
                    try
                    {
                        var mi = GetType().GetMethod(nameof(HandleOperationEvent), BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mi != null)
                        {
                            var handler = Delegate.CreateDelegate(opEvent.EventHandlerType, this, mi);
                            opEvent.AddEventHandler(operation, handler);
                        }
                    }
                    catch (Exception e)
                    { 
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        internal IStructuralEquatable CallbackValues { get; private set; }

        internal enum OperationCallbackType
        {
            None,
            Browse,
            Register,
            Resolve,
            Lookup
        }

        private void HandleOperationEvent(object sender, EventArgs args)
        {
            CallbackValues = null;

            switch (args)
            {
                case BrowseEventArgs oeArgs:
                {
                    CallbackValues = (OperationCallbackType.Browse, oeArgs.EventType, oeArgs.Descriptor.InstanceName, oeArgs.Descriptor.ServiceType, oeArgs.Descriptor.Domain, oeArgs.Descriptor.InterfaceIndex);
                    break;
                }
                case RegistrationEventArgs oeArgs:
                {
                    CallbackValues = (OperationCallbackType.Register, oeArgs.EventType, oeArgs.Descriptor.InstanceName, oeArgs.Descriptor.ServiceType, oeArgs.Descriptor.Domain, oeArgs.Descriptor.InterfaceIndex);
                    break;
                }
                case ResolveEventArgs oeArgs:
                {
                    byte[] trBytes = null;
                    if (oeArgs.TxtRecords != null)
                    {
                        var trb = new TxtRecordBuilder(oeArgs.TxtRecords);
                        trBytes = trb.GetBytes();
                    }

                    CallbackValues = (OperationCallbackType.Resolve, oeArgs.FullName, oeArgs.HostName, oeArgs.Port, trBytes, oeArgs.InterfaceIndex);
                    break;
                }
                case LookupEventArgs oeArgs:
                {
                    CallbackValues = (OperationCallbackType.Lookup, oeArgs.EventType, oeArgs.HostName, oeArgs.IPAddress, oeArgs.Ttl, oeArgs.InterfaceIndex);
                    break;
                }
            }

            _manualResetEvent.Set();
        }

        private void ThrowIfConnectorClosed()
        {
            if (_connectorClosed)
                throw new Exception("The service transport connector was closed unexpectedly");
        }

        // Called by ReceiveLoop, handle exceptions
        // Aka don't assert in these
        private async void ProcessRequestMessage(RequestMessage message)
        {
            if (message.Header.OperationCode == OperationCode.CancelRequest)
                return;

            if (AlternateProcessRequestMessageHandler != null)
                await AlternateProcessRequestMessageHandler(message);
            else
            {
                // See if it's a subordinate request, aka has error return port value in payload
                if (message.Payload?.ErrorReturnPort != null)
                {
                    var ert = _provider.GetErrorReturnConnector(message.Payload.ErrorReturnPort.Value);
                    await ert.SendToRemote(ServiceError.NoError);
                }
                else
                    await _provider.Connector.SendToRemote(ServiceError.NoError);

                if (_callbackMessageUponRequest != null)
                {
                    if (message.Header.SubordinateID != 0)
                        _callbackMessageUponRequest.SetSubordinateID(message.Header.SubordinateID);
                    await _provider.Connector.SendToRemote(_callbackMessageUponRequest.GetBytes());
                }
            }
        }

        private void ServiceTransportConnectorClosed(ConnectionClosedReason reason)
        {
            _connectorClosed = AlternateConnectorClosedHandler == null || AlternateConnectorClosedHandler(reason);
        }

    }
}
