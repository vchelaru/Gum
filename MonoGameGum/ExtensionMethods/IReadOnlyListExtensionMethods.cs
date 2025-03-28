using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.ExtensionMethods;
public static class IReadOnlyListExtensionMethods
{
    public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
    {
        int i = 0;
        foreach (T element in self)
        {
            if (Equals(element, elementToFind))
                return i;
            i++;
        }
        return -1;
    }
}
