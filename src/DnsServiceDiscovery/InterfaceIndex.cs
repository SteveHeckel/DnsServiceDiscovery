using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Mittosoft.DnsServiceDiscovery
{
    public static class InterfaceIndex
    {
        public const uint Any = 0;
        public const uint LocalOnly = uint.MaxValue; // -1 signed
    }
}
