#nullable enable
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Gum;

/// <summary>
/// One layer of the element-name to runtime-type table behind <c>ElementSaveExtensions</c>. There is a
/// process-wide <see cref="Base"/> layer (what <c>[ModuleInitializer]</c>s and other registrations made
/// outside any service write) and one layer per <see cref="GumContext"/>. Every entry carries a global
/// sequence stamp, and a lookup takes the newest entry across the context layer and the base, which gives
/// the same answer one merged table would (later registration wins, factories before types).
/// </summary>
internal sealed class ElementTypeRegistry
{
    private static long _nextStamp;

    /// <summary>The process-wide layer shared by every service.</summary>
    internal static ElementTypeRegistry Base { get; } = new ElementTypeRegistry();

    private readonly Dictionary<string, (Type Type, long Stamp)> _types = new Dictionary<string, (Type, long)>();
    private readonly Dictionary<string, (Func<GraphicalUiElement> Func, long Stamp)> _funcs =
        new Dictionary<string, (Func<GraphicalUiElement>, long)>();
    private (Func<GraphicalUiElement> Func, long Stamp)? _template;

    internal void SetType(string elementName, Type type)
    {
        _types[elementName] = (type, Interlocked.Increment(ref _nextStamp));
    }

    internal void SetFunc(string elementName, Func<GraphicalUiElement> func)
    {
        _funcs[elementName] = (func, Interlocked.Increment(ref _nextStamp));
    }

    internal void SetTemplate(Func<GraphicalUiElement> func)
    {
        _template = (func, Interlocked.Increment(ref _nextStamp));
    }

    internal bool ContainsName(string elementName) => _types.ContainsKey(elementName) || _funcs.ContainsKey(elementName);

    internal void RemoveTypes(Func<Type, bool> shouldRemove)
    {
        List<string>? namesToRemove = null;
        foreach (KeyValuePair<string, (Type Type, long Stamp)> pair in _types)
        {
            if (shouldRemove(pair.Value.Type))
            {
                namesToRemove ??= new List<string>();
                namesToRemove.Add(pair.Key);
            }
        }

        if (namesToRemove != null)
        {
            foreach (string name in namesToRemove)
            {
                _types.Remove(name);
            }
        }
    }

    internal void Clear()
    {
        _types.Clear();
        _funcs.Clear();
        _template = null;
    }

    /// <summary>Finds the newest factory registered under <paramref name="elementName"/> in either layer.</summary>
    internal static bool TryGetFunc(ElementTypeRegistry? layer, string elementName, [NotNullWhen(true)] out Func<GraphicalUiElement>? func)
    {
        long stamp = 0;
        func = null;
        if (layer != null && layer._funcs.TryGetValue(elementName, out (Func<GraphicalUiElement> Func, long Stamp) own))
        {
            func = own.Func;
            stamp = own.Stamp;
        }
        if (Base._funcs.TryGetValue(elementName, out (Func<GraphicalUiElement> Func, long Stamp) shared) && shared.Stamp > stamp)
        {
            func = shared.Func;
        }
        return func != null;
    }

    /// <summary>Finds the newest type registered under <paramref name="elementName"/> in either layer.</summary>
    internal static bool TryGetType(ElementTypeRegistry? layer, string elementName, [NotNullWhen(true)] out Type? type)
    {
        long stamp = 0;
        type = null;
        if (layer != null && layer._types.TryGetValue(elementName, out (Type Type, long Stamp) own))
        {
            type = own.Type;
            stamp = own.Stamp;
        }
        if (Base._types.TryGetValue(elementName, out (Type Type, long Stamp) shared) && shared.Stamp > stamp)
        {
            type = shared.Type;
        }
        return type != null;
    }

    /// <summary>Returns the newest default instantiation function registered in either layer.</summary>
    internal static Func<GraphicalUiElement>? GetTemplate(ElementTypeRegistry? layer)
    {
        (Func<GraphicalUiElement> Func, long Stamp)? own = layer?._template;
        (Func<GraphicalUiElement> Func, long Stamp)? shared = Base._template;
        if (own == null)
        {
            return shared?.Func;
        }
        if (shared == null)
        {
            return own.Value.Func;
        }
        return shared.Value.Stamp > own.Value.Stamp ? shared.Value.Func : own.Value.Func;
    }
}
