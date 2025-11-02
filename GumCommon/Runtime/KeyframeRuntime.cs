using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class KeyframeRuntime
{
    public string? StateName
    {
        get;
        set;
    }

    public string? AnimationName
    {
        get;
        set;
    }
    public string? EventName { get; set; }

    public AnimationRuntime? SubAnimation
    {
        get;
        set;
    }
    public float Time { get; set; }

    public float Length
    {
        get
        {
            if (SubAnimation == null)
            {
                return 0;
            }
            else
            {
                return SubAnimation.Length;
            }
        }
    }
    public InterpolationType InterpolationType { get; set; }
    public Easing Easing { get; set; }

    public override string ToString()
    {
        if(!string.IsNullOrEmpty(StateName))
        {
            return $"{StateName} @ {Time}";
        }
        else if(!string.IsNullOrEmpty(AnimationName))
        {
            return AnimationName!;
        }
        else
        {
            return EventName ?? string.Empty;
        }
    }
}
