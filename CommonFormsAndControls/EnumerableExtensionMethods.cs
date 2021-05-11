using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Linq
{
    public static class EnumerableExtensionMethods
    {
        public static object FirstOrDefault(this IEnumerable enumerable, Func<object, bool> predicate)
        {
            foreach(var item in enumerable)
            {
                if(predicate(item))
                {
                    return item;
                }
            }
            return null;
        }
        public static object FirstOrDefault(this IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                return item;
            }
            return null;
        }
    }
}
