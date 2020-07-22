using System;
using System.Collections.Generic;
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
            if (recordBytes == null)
            {
                throw new ArgumentNullException(nameof(recordBytes));
            }

            if (recordBytes.Length > MaxTxtRecordDataLength)
            {
                throw new ArgumentOutOfRangeException(nameof(recordBytes));
            }

            RecordBytes = recordBytes;
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte> { Convert.ToByte(RecordBytes.Length) };

            bytes.AddRange(RecordBytes);

            return bytes.ToArray();
        }

        public static DnssdTxtRecord Parse(byte[] bytes, ref int index)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (index < 0 || index > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

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
