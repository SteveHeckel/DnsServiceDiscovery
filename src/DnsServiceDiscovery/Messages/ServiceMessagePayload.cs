using System;
using System.Collections.Generic;
using System.Text;

namespace Mittosoft.DnsServiceDiscovery.Messages
{
    public interface IServiceMessagePayload : IByteStreamSerializable
    {
        bool IsSubordinateMessage { get; set; }
    }

    public class ServiceMessagePayload : IServiceMessagePayload
    {
        private readonly byte[] _args = new byte[0];
        public bool IsSubordinateMessage { get; set; }

        public ServiceMessagePayload(params byte[] args)
        {
            this._args = args;
        }

        public ServiceMessagePayload()
        {
        }

        #region IDeviceMessageArgs Members

        public virtual byte[] GetBytes()
        {
            return _args;
        }

        public virtual void Parse(byte[] bytes, ref int index)
        {
            Array.Copy(bytes, index, _args, 0, _args.Length);

            index += _args.Length;
        }

        #endregion
    }

    public class ServiceMessagePayload<T> : IServiceMessagePayload where T : IByteStreamSerializable, new()
    {
        public bool IsSubordinateMessage { get; set; }
        
        public ServiceMessagePayload()
        {
            Value = new T();
        }

        public ServiceMessagePayload(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public byte[] GetBytes()
        {
            return Value.GetBytes();
        }

        public void Parse(byte[] bytes, ref int index)
        {
            Value.Parse(bytes, ref index);
        }
    }
}
