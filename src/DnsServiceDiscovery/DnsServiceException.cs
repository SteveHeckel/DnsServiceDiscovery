using System;

namespace Mittosoft.DnsServiceDiscovery
{
    public class DnsServiceException : Exception
    {
        public ServiceError ServiceError { get; }

        public DnsServiceException(ServiceError error, Exception innerException = null) : base($"DNS Service Exception - Error Code: {error}", innerException)
        {
            ServiceError = error;
        }

        public DnsServiceException(Exception innerException) : base("DNS Service Exception - See InnerExeption", innerException)
        {
        }
    }
}
