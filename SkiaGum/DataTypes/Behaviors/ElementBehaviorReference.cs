using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes.Behaviors
{
    public class ElementBehaviorReference
    {
        /// <summary>
        /// Currently unused - this will eventually be used to reference behaviors
        /// from different Gum projects.
        /// </summary>
        public string ProjectName { get; set; }


        public string BehaviorName { get; set; }

    }
}
