using System;

namespace Mittosoft.DnsServiceDiscovery
{
    public class ServiceDescriptor : IEquatable<ServiceDescriptor>
    {
        public ServiceDescriptor()
        {
        }

        public ServiceDescriptor(string instanceName, string serviceType, string domain, uint interfaceIndex)
        {
            InstanceName = instanceName;
            ServiceType = serviceType;
            Domain = domain;
            InterfaceIndex = interfaceIndex;
        }

        public string InstanceName { get; }
        public string ServiceType { get; }
        public string Domain { get; }
        public uint InterfaceIndex { get; }

        public override string ToString()
        {
            return $"Name=[{InstanceName}], Type=[{ServiceType}], Domain=[{Domain}], Interface=[{InterfaceIndex}]";
        }

        public bool Equals(ServiceDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return InstanceName == other.InstanceName && ServiceType == other.ServiceType && Domain == other.Domain && InterfaceIndex == other.InterfaceIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServiceDescriptor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InstanceName != null ? InstanceName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ServiceType != null ? ServiceType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Domain != null ? Domain.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)InterfaceIndex;
                return hashCode;
            }
        }

        public static bool operator ==(ServiceDescriptor lhs, ServiceDescriptor rhs)
        {
            return lhs?.Equals(rhs) ?? ReferenceEquals(rhs, null);
        }

        public static bool operator !=(ServiceDescriptor lhs, ServiceDescriptor rhs)
        {
            return !(lhs == rhs);
        }
    }
}
