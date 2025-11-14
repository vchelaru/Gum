using System.Collections.Generic;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables;

public class StateSaveCategory
{
    public string Name
    {
        get;
        set;
    }

    [XmlElement("State")]
    public List<StateSave> States
    {
        get;
        set;
    }


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

        foreach(var state in this.States)
        {
            var clone = state.Clone();
            toReturn.States.Add(clone);
        }

        return toReturn;
    }
}
