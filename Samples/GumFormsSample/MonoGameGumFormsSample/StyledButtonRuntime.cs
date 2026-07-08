using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Gum.Forms.DefaultVisuals.V3;

namespace GumFormsSample
{
    class StyledButtonRuntime : ButtonVisual
    {
        public StyledButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
            base(fullInstantiation, tryCreateFormsObject)
        {
            if(fullInstantiation)
            {
                // V3's states are computed from BackgroundColor/ForegroundColor rather than
                // per-state variables, so a custom look only needs the base color set.
                BackgroundColor = new Color(255, 100, 194);
            }
        }
    }
}
