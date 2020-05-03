using System;
using System.Collections.Generic;
using System.Text;

namespace Mittosoft.DnsServiceDiscovery.Messages
{
    public interface IByteStreamSerializable
    {
        byte[] GetBytes();

        void Parse(byte[] bytes, ref int index);
    }
}
