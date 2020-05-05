using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Mittosoft.DnsServiceDiscovery.Helpers;

namespace Mittosoft.DnsServiceDiscovery.Records
{
    // TXT Record Format for DNS-SD
    public class DnssdTxtRecord
    {
        public const int MaxTxtRecordDataLength = byte.MaxValue;

        internal byte[] RecordBytes { get; }

        public DnssdTxtRecord(byte[] recordBytes)
        {
            Guard.Against.NullOrTooManyElements(recordBytes, nameof(recordBytes), MaxTxtRecordDataLength);

            RecordBytes = recordBytes;
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte> {Convert.ToByte(RecordBytes.Length)};

            bytes.AddRange(RecordBytes);

            return bytes.ToArray();
        }

        public static DnssdTxtRecord Parse(byte[] bytes, ref int index)
        {
            Guard.Against.Null(bytes, nameof(bytes));
            Guard.Against.OutOfRange(index, nameof(index), 0, bytes.Length);

            if (index == bytes.Length)
                return null;

            var length = bytes[index++];
            
            var txtRecord = new DnssdTxtRecord(bytes.SubArray(index, length));
            index += length;

            return txtRecord;
        }

        public override string ToString()
        {
            return TxtRecordBuilder.ToRecordString(this);
        }
    }
}
