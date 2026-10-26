using System;

namespace Gum.DataTypes;

/// <summary>
/// Names the Gum element (Screen or Component) a generated class is built from, such as
/// <c>[GumElement("Controls/ButtonClose")]</c>. Code generation emits it from syntax version 7, so a
/// tool can read which elements a game's code uses without running the game.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GumElementAttribute : Attribute
{
    /// <summary>The element's name, including its folder, as <c>ObjectFinder.GetElementSave</c> takes it.</summary>
    public string ElementName { get; }

    /// <summary>Initializes a new <see cref="GumElementAttribute"/>.</summary>
    public GumElementAttribute(string elementName)
    {
        ElementName = elementName;
    }
}
