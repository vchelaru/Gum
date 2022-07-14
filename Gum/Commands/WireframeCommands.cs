using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Commands
{
    public class WireframeCommands
    {
        public void Refresh(bool forceLayout = true, bool forceReloadContent = false)
        {
            WireframeObjectManager.Self.RefreshAll(forceLayout, forceReloadContent);
        }

        public void RefreshGuides()
        {
            WireframeObjectManager.Self.RefreshGuides();
        }
    }
}
