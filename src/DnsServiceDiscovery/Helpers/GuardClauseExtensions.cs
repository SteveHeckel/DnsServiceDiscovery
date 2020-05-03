using System;
using System.Collections;
using Ardalis.GuardClauses;

namespace Mittosoft.DnsServiceDiscovery.Helpers
{
    public static class GuardClausesExtensions
    {
        public static void NullOrTooManyElements(this IGuardClause guardClause, IList input,  string parameterName, int maxCount)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count > maxCount)
                throw new ArgumentException($"Parameter [{parameterName}] contains too many elements, Max is {maxCount}");
        }

        public static void NullOrIncorrectElementCount(this IGuardClause guardClause, IList input, string parameterName, int count)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count != count)
                throw new ArgumentException($"Parameter [{parameterName}] does not contain the correct number of elements: {count}");
        }

        public static void NullOrNotEnoughElements(this IGuardClause guardClause, IList input, string parameterName, int count)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count < count)
                throw new ArgumentException($"Parameter [{parameterName}] does not contain the correct number of elements: {count}");
        }

        public static void NotAnyFlag<TEnum>(this IGuardClause guardClause, TEnum input, TEnum flags, string parameterName) where TEnum : struct, Enum, IConvertible
        {
            Guard.Against.Null(parameterName, nameof(parameterName));

            if (!input.HasAnyFlag(flags))
                throw new ArgumentException($"Parameter [{parameterName}] does not contain at least one flag from {flags}");
        }
    }
}
