using System;

namespace Gum.DataTypes;

/// <summary>
/// Names a Gum element a game's own code uses. The Gum.Analyzers source generator emits one per
/// element on the game assembly, and <c>gumcli pack --shake --roots-assembly</c> keeps those elements.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GumElementRootAttribute : Attribute
{
    /// <summary>The element's name, including its folder.</summary>
    public string ElementName { get; }

    /// <summary>Initializes a new <see cref="GumElementRootAttribute"/>.</summary>
    public GumElementRootAttribute(string elementName)
    {
        ElementName = elementName;
    }
}
