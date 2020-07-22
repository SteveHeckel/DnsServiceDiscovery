using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mittosoft.DnsServiceDiscovery.Helpers
{
    public static class ArrayExtensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);

            return result;
        }

        //public static bool CheckEquality<T>(this T[] first, T[] second)
        //{
        //    return first.SequenceEqual(second);
        //}
    }

    public static class NetworkExtensions
    {
        public static async Task<int> ReadAsyncWithTimeout(this NetworkStream stream, byte[] buffer, int offset, int count)
        {
            if (stream.CanRead)
            {

                var readTask = stream.ReadAsync(buffer, offset, count);
                var delayTask = Task.Delay(stream.ReadTimeout);
                var task = await Task.WhenAny(readTask, delayTask);

                if (task == readTask)
                    return await readTask;
            }

            return 0;
        }
    }
}
