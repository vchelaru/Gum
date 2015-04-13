using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.ToolCommands
{
    public class GuiCommands
    {
        Wireframe.WireframeControl mWireframeControl;

        static GuiCommands mSelf = new GuiCommands();


        public static GuiCommands Self
        {
            get { return mSelf; }
            set { mSelf = value; }
        }

        public void Initialize(Wireframe.WireframeControl wireframeControl)
        {
            mWireframeControl = wireframeControl;
        }

        public void UpdateWireframeToProject()
        {
            mWireframeControl.UpdateToProject();
        }

        public void RefreshWireframe()
        {
            mWireframeControl.RefreshDisplay();
        }
    }
}
