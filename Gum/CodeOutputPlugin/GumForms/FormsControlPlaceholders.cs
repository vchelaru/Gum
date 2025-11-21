using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// As of September 21, 2025, this project is using .NET 4.7.
// As such, it cannot link MonoGameGum .dll where Forms controls
// exist, but this needs access to those forms controls to know if
// an exposed variable should be override. This could eventually get
// resolved in a few ways:
// 1. Move to .NET 8+, but this is a long road
// 2. Move Forms controls to GumCommon, which may happen but this requires
//    FlatRedBall to update its dependencies first.
// In the meantime, these classes exists as a placeholder to allow code generation
// to properly override classes

namespace Gum.Forms.Controls
{

    public class Button
    {
        public virtual string Text { get; set; }
    }

    public class MenuItem
    {
        public virtual string Header { get; set; }
    }

}


