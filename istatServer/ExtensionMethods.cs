using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace istatServer
{
    public static class ExtensionMethods
    {
        public static IEnumerable<T> Slice<T>(this IEnumerable<T> sequence, int firstIndex, int length)
        {
            return sequence.Skip(firstIndex).Take(length);
        }
    }
}
