using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateAnimationPlugin.SaveClasses
{
    public class AnimationSave
    {
        public bool Loops { get; set; }

        public string Name { get; set; }

        public List<AnimatedStateSave> States { get; set; }
        public List<AnimationReferenceSave> Animations { get; set; }
        public AnimationSave()
        {
            States = new List<AnimatedStateSave>();
            Animations = new List<AnimationReferenceSave>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
