using FlatRedBall.Glue.StateInterpolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateAnimationPlugin.SaveClasses
{
    public class AnimatedStateSave
    {
        public string StateName { get; set; }

        public float Time { get; set; }

        public InterpolationType InterpolationType { get; set; }
        public Easing Easing { get; set; }        
    }
}
