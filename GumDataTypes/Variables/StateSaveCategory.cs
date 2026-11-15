using System.Collections.Generic;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables;

public class StateSaveCategory
{
    public string Name
    {
        get;
        set;
    } = string.Empty;

    [XmlElement("State")]
    public List<StateSave> States
    {
        get;
        set;
    } = new List<StateSave>();

    /// <summary>
    /// The name of the palette color the tool marks this category with (for example "Indigo"), or null
    /// for none. Editor-only: the runtime and code generation ignore it, and a name this build does not
    /// know is kept as is so a newer tool's color survives a save.
    /// </summary>
    public string? ColorName
    {
        get;
        set;
    }

    public bool ShouldSerializeColorName() => !string.IsNullOrEmpty(ColorName);

    public StateSaveCategory()
    {
        States = new List<StateSave>();
    }

    public override string ToString()
    {
        return Name;
    }

    public StateSaveCategory Clone()
    {
        var toReturn = new StateSaveCategory();

        toReturn.Name = this.Name;
        toReturn.ColorName = this.ColorName;

        foreach(var state in this.States)
        {
            var clone = state.Clone();
            toReturn.States.Add(clone);
        }

        return toReturn;
    }
}

internal static class StateSaveListExtensions
{
    /// <summary>
    /// Returns the first state whose name matches <paramref name="name"/> (ordinal, case-sensitive),
    /// or null. Same result as <c>FirstOrDefault(item => item.Name == name)</c> without allocating a
    /// closure, which matters because Forms looks states up on every hover and press.
    /// </summary>
    internal static StateSave? FindByName(this List<StateSave> states, string name)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i].Name == name)
            {
                return states[i];
            }
        }
        return null;
    }
}
