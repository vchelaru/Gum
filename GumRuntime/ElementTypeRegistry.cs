#nullable enable
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Gum;

/// <summary>
/// One layer of the element-name to runtime-type table behind <c>ElementSaveExtensions</c>. There is a
/// process-wide <see cref="Base"/> layer (what <c>[ModuleInitializer]</c>s, the optional-package hooks and
/// registrations made while no service exists write) and one layer per <see cref="GumContext"/>. A lookup
/// checks the service's own layer first, then the base.
/// </summary>
internal sealed class ElementTypeRegistry
{
    /// <summary>The process-wide layer shared by every service.</summary>
    internal static ElementTypeRegistry Base { get; } = new ElementTypeRegistry();

    private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
    private readonly Dictionary<string, Func<GraphicalUiElement>> _funcs = new Dictionary<string, Func<GraphicalUiElement>>();
    private Func<GraphicalUiElement>? _template;

    internal void SetType(string elementName, Type type) => _types[elementName] = type;

    internal void SetFunc(string elementName, Func<GraphicalUiElement> func) => _funcs[elementName] = func;

    internal void SetTemplate(Func<GraphicalUiElement> func) => _template = func;

    internal bool ContainsName(string elementName) => _types.ContainsKey(elementName) || _funcs.ContainsKey(elementName);

    internal void RemoveTypes(Func<Type, bool> shouldRemove)
    {
        List<string>? namesToRemove = null;
        foreach (KeyValuePair<string, Type> pair in _types)
        {
            if (shouldRemove(pair.Value))
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

    /// <summary>Finds a factory registered under <paramref name="elementName"/>: the service's layer, then the base.</summary>
    internal static bool TryGetFunc(ElementTypeRegistry? layer, string elementName, [NotNullWhen(true)] out Func<GraphicalUiElement>? func)
    {
        if (layer != null && layer._funcs.TryGetValue(elementName, out func))
        {
            return true;
        }
        return Base._funcs.TryGetValue(elementName, out func);
    }

    /// <summary>Finds a type registered under <paramref name="elementName"/>: the service's layer, then the base.</summary>
    internal static bool TryGetType(ElementTypeRegistry? layer, string elementName, [NotNullWhen(true)] out Type? type)
    {
        if (layer != null && layer._types.TryGetValue(elementName, out type))
        {
            return true;
        }
        return Base._types.TryGetValue(elementName, out type);
    }

    /// <summary>Returns the default instantiation function: the service's, else the base's.</summary>
    internal static Func<GraphicalUiElement>? GetTemplate(ElementTypeRegistry? layer) => layer?._template ?? Base._template;
}
