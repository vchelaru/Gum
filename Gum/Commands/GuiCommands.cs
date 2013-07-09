using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gum.Commands
{
    public class GuiCommands
    {
        FlowLayoutPanel mFlowLayoutPanel;
        internal void Initialize(MainWindow mainWindow)
        {
            mFlowLayoutPanel = mainWindow.ToolbarPanel;
        }

        public void AddControl(Control control)
        {
            mFlowLayoutPanel.Controls.Add(control);
        }
    }
}
