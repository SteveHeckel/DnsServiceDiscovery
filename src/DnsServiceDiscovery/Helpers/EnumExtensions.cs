using System;

namespace Mittosoft.DnsServiceDiscovery.Helpers
{
    internal static class EnumExtensions
    {
        public static bool HasAnyFlag<TEnum>(this TEnum value, TEnum flags) where TEnum : struct, Enum, IConvertible
        {
            var lvalue = value.ToInt64(null);
            var lflags = flags.ToInt64(null);

            return (lvalue & lflags) != 0;
        }

        public static bool HasAllFlags<TEnum>(this TEnum value, TEnum flags) where TEnum : struct, Enum, IConvertible
        {
            var lvalue = value.ToInt64(null);
            var lflags = flags.ToInt64(null);

            return (lvalue & lflags) == lflags;
        }
    }
}
