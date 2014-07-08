using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateAnimationPlugin.SaveClasses
{
    public class AnimationSave
    {
        public string Name { get; set; }

        public List<AnimatedStateSave> States { get; set; }

        public AnimationSave()
        {
            States = new List<AnimatedStateSave>();
        }
    }
}
