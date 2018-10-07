using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.DataTypes;

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

        public TabPage AddControl(System.Windows.Controls.UserControl control, string tabTitle, TabLocation tabLocation = TabLocation.Center)
        {
            return mMainWindow.AddWpfControl(control, tabTitle, tabLocation);
        }
        
        public void RemoveControl(System.Windows.Controls.UserControl control)
        {
            mMainWindow.RemoveWpfControl(control);
        }

        public void ShowControl(System.Windows.Controls.UserControl control)
        {
            mMainWindow.ShowTabForControl(control);
        }

        public void PrintOutput(string output)
        {
            OutputManager.Self.AddOutput(output);
        }

        public void RefreshElementTreeView()
        {
            ElementTreeViewManager.Self.RefreshUi();
        }

        public void RefreshElementTreeView(ElementSave element)
        {
            ElementTreeViewManager.Self.RefreshUi(element);
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
