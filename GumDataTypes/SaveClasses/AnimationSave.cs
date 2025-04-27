using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.StateAnimation.SaveClasses;

/// <summary>
/// A serializable animation which contains a list of States (keyframes), Animations (sub-animations), and Events (timed named events)
/// </summary>
public class AnimationSave
{
    public bool Loops { get; set; }

    public string Name { get; set; }

    public List<AnimatedStateSave> States { get; set; } = new List<AnimatedStateSave>();
    public List<AnimationReferenceSave> Animations { get; set; } = new List<AnimationReferenceSave>();
    public List<NamedEventSave> Events { get; set; } = new List<NamedEventSave>();
    

    public override string ToString()
    {
        return Name;
    }
}
