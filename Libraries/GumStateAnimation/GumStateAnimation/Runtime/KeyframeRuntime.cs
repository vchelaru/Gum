using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class KeyframeRuntime
{
    public StateSave CachedCumulativeState
    {
        get;
        set;
    }

    public string StateName
    {
        get;
        set;
    }

    public string AnimationName
    {
        get;
        set;
    }
    public string EventName { get; set; }

    public AnimationRuntime SubAnimation
    {
        get;
        set;
    }
    public float Time { get; set; }
    public InterpolationType InterpolationType { get; internal set; }
    public Easing Easing { get; internal set; }
}
