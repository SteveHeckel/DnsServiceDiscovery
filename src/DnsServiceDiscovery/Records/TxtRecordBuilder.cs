using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mittosoft.DnsServiceDiscovery.Records
{
    public class TxtRecordBuilder
    {
        public List<TxtRecord> TxtRecords { get; } = new List<TxtRecord>();

        public TxtRecordBuilder()
        {
        }

        public TxtRecordBuilder(IEnumerable<TxtRecord> recordList)
        {
            TxtRecords.AddRange(recordList);
        }

        public TxtRecordBuilder(IEnumerable<string> txtRecordStrings)
        {
            foreach (var txtRecordString in txtRecordStrings)
            {
                Append(txtRecordString);
            }
        }

        public TxtRecordBuilder(string txtRecordString)
        {
            Append(txtRecordString);
        }

        public void Append(string txtRecordString)
        {
            Append(ParseString(txtRecordString));
        }

        public void Append(TxtRecord record)
        {
            TxtRecords.Add(record);
        }

        public const int BonjourMaxAssembledTxtRecordLength = 2048;

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();

            foreach (var txtRecord in TxtRecords)
                bytes.AddRange(txtRecord.GetBytes());

            return bytes.ToArray();
        }

        // Parse binary array in to constituent TxtRecord instances
        public int Parse(byte[] bytes, int index)
        {
            TxtRecord txtRecord = null;

            do
            {
                txtRecord = TxtRecord.Parse(bytes, ref index);
                if (txtRecord != null)
                    Append(txtRecord);
            } while (txtRecord != null);

            return index;
        }
       
        // This method parses a bonjour dns-sd style TXT record command line string and creates a TxtRecord instance
        public static TxtRecord ParseString(string txtRecordString)
        {
            var txtRecordData = new List<byte>();

            int incr;
            for (var i = 0; i < txtRecordString.Length && txtRecordData.Count < TxtRecord.MaxTxtRecordDataLength; i += incr)
            {
                // Not an escape char or it's the last char of the string
                if (txtRecordString[i] != '\\' || i + 1 == txtRecordString.Length)
                {
                    txtRecordData.Add(Convert.ToByte(txtRecordString[i]));
                    incr = 1;
                }
                // The original dns-sd code for this didn't check to ensure the source string was long enough to index to i + 2 and 3
                // We can safely index to i + 1 due to the first condition above
                // else is it a hex pair escape sequence? e.g. "\03A"
                else if (txtRecordString[i + 1] == 'x' && txtRecordString.Length >= i + 4 && IsHexDigit(txtRecordString[i + 2]) && IsHexDigit(txtRecordString[i + 3]))
                {
                    txtRecordData.Add(HexPair(txtRecordString.Substring(i + 2, 2)));
                    incr = 4;
                }
                // Regular escape, just add the char following it
                else
                {
                    txtRecordData.Add(Convert.ToByte(txtRecordString[i + 1]));
                    incr = 2;
                }
            }

            return new TxtRecord(txtRecordData.ToArray());
        }

        public static string ToRecordString(TxtRecord txtRecord)
        {
            var builder = new StringBuilder();

            foreach (var value in txtRecord.Record)
            {
                if (IsPrintable(value))
                    builder.Append(Convert.ToChar(value));
                else
                    builder.Append($"\\x{value:X2}");
            }

            return builder.ToString();
        }

        public static bool IsPrintable(byte value)
        {
            return (value >= 0x20 && value <= 0x7E);
        }
        
        private static bool IsHexDigit(char c)
        {
            return ((c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f') ||
                     (c >= 'A' && c <= 'F'));
        }

        private static byte HexPair(string digits)
        {
            if (digits.Length != 2)
                throw new ArgumentException($"{nameof(digits)} string must have a length of 2");

            return byte.Parse(digits, NumberStyles.HexNumber);
        }
    }
}
