using System;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes;

/// <summary>
/// For code that works on elements of a loaded project. <see cref="ElementSave.DefaultState"/> is
/// null only for an element with no states, and loading a project (GumProjectSave.Initialize) or
/// creating an element gives every element its default state.
/// </summary>
public static class ElementSaveDefaultStateExtensions
{
    /// <summary>The element's default state; throws if the element has no states.</summary>
    public static StateSave GetDefaultStateOrThrow(this ElementSave element) =>
        element.DefaultState ?? throw new InvalidOperationException($"{element} has no default state.");
}
