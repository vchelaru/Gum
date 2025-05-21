using FlatRedBall.Glue.StateInterpolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.StateAnimation.SaveClasses;

public class AnimatedStateSave
{
    public string StateName { get; set; }

    public float Time { get; set; }

    public InterpolationType InterpolationType { get; set; }
    public Easing Easing { get; set; }   
 
    public AnimatedStateSave()
    {
        Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out;
        InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
    }

    public override string ToString()
    {
        return StateName + " " + Time;
    }
}
