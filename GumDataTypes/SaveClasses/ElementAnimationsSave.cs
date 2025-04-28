using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.StateAnimation.SaveClasses;

public class ElementAnimationsSave
{
    public List<AnimationSave> Animations
    {
        get;
        set;
    } = new List<AnimationSave>();

    public string ElementName
    {
        get; set;
    }

    public ElementAnimationsSave()
    {
        Animations = new List<AnimationSave>();
    }

    public override string ToString()
    {
        return $"{ElementName} ({Animations.Count} animations)";
    }
}
