#if !FRB
using System;
using System.Collections.Generic;

namespace MonoGameGum.ExtensionMethods;

/// <summary>
/// Compatibility shim. The real type lives in <see cref="Gum.ExtensionMethods.IReadOnlyListExtensionMethods"/>.
/// This forwarding class exists so existing user code with <c>using MonoGameGum.ExtensionMethods;</c>
/// continues to compile during the deprecation window. A static extension class cannot be
/// subclassed, so each member forwards to the real implementation instead.
/// </summary>
[Obsolete("Use Gum.ExtensionMethods.IReadOnlyListExtensionMethods instead. The MonoGameGum.ExtensionMethods namespace will be removed in a future release.")]
public static class IReadOnlyListExtensionMethods
{
    /// <summary>
    /// Obsolete. Use <see cref="Gum.ExtensionMethods.IReadOnlyListExtensionMethods.IndexOf{T}(IReadOnlyList{T}, T)"/>.
    /// </summary>
    // The body is intentionally a self-contained copy rather than a forward to the Gum.* method:
    // forwarding would re-import the IndexOf extension surface into this (obsolete) namespace and
    // create an ambiguous call. This shim is removed when the MonoGameGum.* namespace is retired.
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
#endif
