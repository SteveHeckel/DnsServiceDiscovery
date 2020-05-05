using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Mittosoft.DnsServiceDiscovery.Records;

namespace Mittosoft.DnsServiceDiscovery
{
    public class ServiceOperationEventArgs : EventArgs
    {
        public bool MoreComing { get; }

        public ServiceOperationEventArgs(bool moreComing)
        {
            MoreComing = moreComing;
        }
    }
    
    public enum BrowseEventType
    {
        None,
        Added,
        Removed
    }

    public class BrowseEventArgs : ServiceOperationEventArgs
    {
        public BrowseEventArgs(BrowseEventType eventType, ServiceDescriptor descriptor, bool moreComing)
            : base(moreComing)
        {
            EventType = eventType;
            Descriptor = descriptor;
        }

        public BrowseEventType EventType { get; }
        public ServiceDescriptor Descriptor { get; }
    }

    public enum LookupEventType
    {
        None,
        Added,
        Removed,
        Timeout,
        NoSuchRecord,
        Error
    }

    public class LookupEventArgs : ServiceOperationEventArgs
    {
        public LookupEventType EventType { get; }
        public string HostName { get; }
        public IPAddress IPAddress { get; }
        public uint Ttl { get; }
        public uint InterfaceIndex { get; }

        public LookupEventArgs(LookupEventType eventType, string name, IPAddress ipAddress, uint ttl, uint interfaceIndex, bool moreComing)
            : base(moreComing)
        {
            EventType = eventType;
            HostName = name;
            IPAddress = ipAddress;
            Ttl = ttl;
            InterfaceIndex = interfaceIndex;
        }
    }

    public enum RegistrationEventType
    {
        Added,
        Removed,
        Error
    }

    public class RegistrationEventArgs : ServiceOperationEventArgs
    {
        public RegistrationEventType EventType { get; }
        public ServiceDescriptor Descriptor { get; }
        public ServiceError Error { get; }

        public RegistrationEventArgs(RegistrationEventType eventType, ServiceDescriptor descriptor, ServiceError error, bool moreComing)
            : base(moreComing)
        {
            EventType = eventType;
            Descriptor = descriptor;
            Error = error;
        }
    }

    public class ResolveEventArgs : ServiceOperationEventArgs
    {
        public uint InterfaceIndex { get; }
        public string FullName { get; }
        public string HostName { get; }
        public ushort Port { get; }
        public IReadOnlyList<DnssdTxtRecord> TxtRecords { get; }

        public ResolveEventArgs(string fullName, string hostName, ushort port, uint interfaceIndex, IEnumerable<DnssdTxtRecord> txtRecords, bool moreComing)
            : base(moreComing)
        {
            FullName = fullName;
            HostName = hostName;
            Port = port;
            InterfaceIndex = interfaceIndex;
            if (txtRecords != null)
                TxtRecords = txtRecords.ToList().AsReadOnly();
        }

        public override string ToString()
        {
            var sb = new StringBuilder($"Full Name=[{FullName}], Host=[{HostName}], Port=[{Port}], Interface=[{InterfaceIndex}]\n");
            if (TxtRecords != null)
            {
                sb.Append("TXT Records:\n");
                foreach (var txtRecord in TxtRecords)
                    sb.Append($"[{txtRecord}]\n");
            }

            return sb.ToString();
        }
    }

}
