using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Mittosoft.DnsServiceDiscovery.Helpers;

namespace Mittosoft.DnsServiceDiscovery.Messages
{
    internal interface IServiceMessage : IByteStreamSerializable
    {
        IServiceMessageHeader Header { get; }
        IServiceMessagePayload Payload { get; }
    }

    internal interface IServiceMessage<out TPayload> where TPayload : class, IServiceMessagePayload, new()
    {
        TPayload Payload { get; }
    }

    internal class ServiceMessage<TPayload> : ServiceMessage where TPayload : class, IServiceMessagePayload, new()
    {
        public new TPayload Payload => base.Payload as TPayload;

        public ServiceMessage() : this(new ServiceMessageHeader(), new TPayload())
        {
        }

        public ServiceMessage(OperationCode opCode) : this(new ServiceMessageHeader(opCode), new TPayload())
        {
        }

        public ServiceMessage(ServiceMessageHeader header, TPayload payload) : base(header, payload)
        {
        }
    }

    internal class ServiceMessage : IByteStreamSerializable
    {
        private readonly ServiceMessageHeader _header;
        public IServiceMessageHeader Header => _header;
        public IServiceMessagePayload Payload { get; }

        public ServiceMessage() : this(new ServiceMessageHeader(), new ServiceMessagePayload())
        {
        }

        public ServiceMessage(OperationCode opCode) : this(new ServiceMessageHeader(opCode), new ServiceMessagePayload())
        {
        }

        public ServiceMessage(ServiceMessageHeader header) : this(header, new ServiceMessagePayload())
        { }

        public ServiceMessage(ServiceMessageHeader header, IServiceMessagePayload payload)
        {
            _header = header ?? throw new ArgumentNullException(nameof(header));
            Payload = payload;
            if (Payload != null)
                Payload.IsSubordinateMessage = _header.SubordinateID != 0;
        }

        public void SetSubordinateID(ulong subordinateID)
        {
            _header.SubordinateID = subordinateID;
            if (Payload != null)
                Payload.IsSubordinateMessage = subordinateID != 0;
        }

        public byte[] GetBytes()
        {
            var messageBytes = new List<byte>();

            var payloadBytes = Payload?.GetBytes();

            _header.DataLength = (uint)(payloadBytes?.Length ?? 0);
            var headerBytes = _header.GetBytes();

            messageBytes.AddRange(headerBytes);

            if (payloadBytes != null)
                messageBytes.AddRange(payloadBytes);

            return messageBytes.ToArray();
        }

        public int Parse(byte[] bytes, int index)
        {
            Parse(bytes, ref index);

            return index;
        }

        public void Parse(byte[] bytes, ref int index)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (index < 0 || index > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

            ((IByteStreamSerializable)Header).Parse(bytes, ref index);

            if (Header.DataLength != 0)
                Payload.Parse(bytes, ref index);
        }

        //
        // Static Helper methods
        //
        public static string GetString(byte[] buffer, ref int startIndex)
        {
            string result = null;

            var index = Array.FindIndex(buffer, startIndex, b => b == 0);
            if (index >= 0)
            {
                var strLen = index - startIndex;
                result = Encoding.ASCII.GetString(buffer, startIndex, strLen);
                startIndex += strLen + 1;
            }

            return result;
        }

        public static byte[] GetSubArray(byte[] buffer, ref int index, int length)
        {
            var bytes = buffer.SubArray(index, length);
            index += length;

            return bytes;
        }

        public static byte[] GetMessageStringBytes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new byte[] { 0 };

            var bytes = new List<byte>(Encoding.ASCII.GetBytes(value)) { 0 };

            return bytes.ToArray();
        }

        public static ushort GetUInt16(byte[] buffer, ref int index)
        {
            var value = NetworkToHostOrder(BitConverter.ToUInt16(buffer, index));
            index += sizeof(ushort);

            return value;
        }

        public static uint GetUInt32(byte[] buffer, ref int index)
        {
            var value = NetworkToHostOrder(BitConverter.ToUInt32(buffer, index));
            index += sizeof(uint);

            return value;
        }

        // Signed integers
        public static short HostToNetworkOrder(short vale)
        {
            return (short)IPAddress.HostToNetworkOrder((short)vale);
        }

        public static int HostToNetworkOrder(int vale)
        {
            return (int)IPAddress.HostToNetworkOrder((int)vale);
        }

        public static long HostToNetworkOrder(long vale)
        {
            return (long)IPAddress.HostToNetworkOrder((long)vale);
        }

        public static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }

        public static int NetworkToHostOrder(int network)
        {
            return HostToNetworkOrder(network);
        }

        public static short NetworkToHostOrder(short network)
        {
            return HostToNetworkOrder(network);
        }

        // Unsigned integers
        public static ushort HostToNetworkOrder(ushort value)
        {
            return (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public static uint HostToNetworkOrder(uint value)
        {
            return (uint)IPAddress.HostToNetworkOrder((int)value);
        }

        public static ulong HostToNetworkOrder(ulong value)
        {
            return (ulong)IPAddress.HostToNetworkOrder((long)value);
        }

        public static ulong NetworkToHostOrder(ulong network)
        {
            return HostToNetworkOrder(network);
        }

        public static uint NetworkToHostOrder(uint network)
        {
            return HostToNetworkOrder(network);
        }

        public static ushort NetworkToHostOrder(ushort network)
        {
            return HostToNetworkOrder(network);
        }

    }
}
