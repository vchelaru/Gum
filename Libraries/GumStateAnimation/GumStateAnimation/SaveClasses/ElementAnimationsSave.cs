using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateAnimationPlugin.SaveClasses
{
    public class ElementAnimationsSave
    {
        public List<AnimationSave> Animations
        {
            get;
            set;
        }

        public ElementAnimationsSave()
        {
            Animations = new List<AnimationSave>();
        }
    }
}
