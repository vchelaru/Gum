using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Gum.StateAnimation.SaveClasses;

public class AnimationReferenceSave
{
    public string Name
    {
        get;
        set;
    }

    public float Time
    {
        get;
        set;
    }

    [XmlIgnore]
    public string SourceObject
    {
        get
        {
            if(Name.Contains('.'))
            {
                return Name.Substring(0, Name.IndexOf('.'));
            }
            else
            {
                return null;
            }
        }
    }

    [XmlIgnore]
    public string RootName
    {
        get
        {
            if (Name.Contains('.'))
            {
                int indexOfDot = Name.IndexOf('.');
                int startingIndex = indexOfDot + 1;

                return Name.Substring(startingIndex, Name.Length - startingIndex);
            }
            else
            {
                return Name;
            }
        }
    }

    public override string ToString()
    {
        return Name + " " + Time;
    }
}
