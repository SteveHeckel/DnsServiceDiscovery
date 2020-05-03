using System;
using System.Collections.Generic;
using Ardalis.GuardClauses;
using Mittosoft.DnsServiceDiscovery.Helpers;

namespace Mittosoft.DnsServiceDiscovery.Records
{
    // TXT Record Format for DNS-SD
    public class TxtRecord
    {
        public const int MaxTxtRecordDataLength = byte.MaxValue;

        internal byte[] Record { get; }

        public TxtRecord(byte[] record)
        {
            Guard.Against.NullOrTooManyElements(record, nameof(record), MaxTxtRecordDataLength);

            Record = record;
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte> {Convert.ToByte(Record.Length)};

            bytes.AddRange(Record);

            return bytes.ToArray();
        }

        public static TxtRecord Parse(byte[] bytes, ref int index)
        {
            // Todo: need to do some bounds checking on this
            if (index >= bytes.Length)
                return null;

            var length = bytes[index++];
            
            var txtRecord = new TxtRecord(bytes.SubArray(index, length));
            index += length;

            return txtRecord;
        }

        public override string ToString()
        {
            return TxtRecordBuilder.ToRecordString(this);
        }
    }
}
