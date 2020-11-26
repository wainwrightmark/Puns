using System;
using System.Collections.Generic;

namespace Puns
{
    public static class Extensions
    {
        public static int LastIndexOf<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                    return i;
            }

            return -1;
        }
    }
}