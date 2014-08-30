using Gum.Managers;
using Gum.ToolStates;
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

        MainWindow mMainWindow;

        internal void Initialize(MainWindow mainWindow)
        {
            mFlowLayoutPanel = mainWindow.ToolbarPanel;

            mMainWindow = mainWindow;
        }

        public void AddControl(Control control)
        {
            mFlowLayoutPanel.Controls.Add(control);
        }

        internal void RefreshStateTreeView()
        {
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
            
        }

        internal void RefreshPropertyGrid(bool force = false)
        {
            PropertyGridManager.Self.RefreshUI(force:force);
        }

        public void AddControl(System.Windows.Controls.UserControl control, string tabTitle)
        {
            mMainWindow.AddWpfControl(control, tabTitle);

        }

        public void PrintOutput(string output)
        {
            OutputManager.Self.AddOutput(output);
        }
    }
}
