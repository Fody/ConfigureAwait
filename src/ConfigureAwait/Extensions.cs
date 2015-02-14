using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigureAwait
{
    internal static class Extensions
    {
        public static int SearchIndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var i = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    return i;
                i++;
            }
            return -1;
        }
    }
}