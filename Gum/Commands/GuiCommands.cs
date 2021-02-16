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

        internal void RefreshStateTreeView()
        {
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
        }

        public void RefreshPropertyGrid(bool force = false)
        {
            PropertyGridManager.Self.RefreshUI(force:force);
        }

        /// <summary>
        /// Refreshes the displayed values without clearing and recreating the grid
        /// </summary>
        public void RefreshPropertyGridValues()
        {
            PropertyGridManager.Self.RefreshVariablesDataGridValues();
        }

        public TabPage AddControl(System.Windows.Controls.UserControl control, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
        {
            CheckForInitialization();
            return mMainWindow.AddWpfControl(control, tabTitle, tabLocation);
        }

        public TabPage AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation )
        {
            CheckForInitialization();
            return mMainWindow.AddWinformsControl(control, tabTitle, tabLocation);
        }

        private void CheckForInitialization()
        {
            if(mMainWindow == null)
            {
                throw new InvalidOperationException("Need to call Initialize first");
            }
        }

        public TabPage AddWinformsControl(Control control, string tabTitle, TabLocation tabLocation)
        {
            return mMainWindow.AddWinformsControl(control, tabTitle, tabLocation);
        }
        
        public void PositionWindowByCursor(System.Windows.Window window)
        {
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

            double width = window.Width;
            if (double.IsNaN(width))
            {
                width = 0;
            }
            double height = window.Height;
            if (double.IsNaN(height))
            {
                height = 0;
            }

            var mousePosition = GumCommands.Self.GuiCommands.GetMousePosition();
            window.Left = mousePosition.X - width / 2;
            window.Top = mousePosition.Y - height / 2;
        }


        public void PositionWindowByCursor(System.Windows.Forms.Form window)
        {
            var mousePosition = GumCommands.Self.GuiCommands.GetMousePosition();

            window.Location = new System.Drawing.Point(mousePosition.X - window.Width / 2, mousePosition.Y - window.Height / 2);
        }

        public void RemoveControl(System.Windows.Controls.UserControl control)
        {
            mMainWindow.RemoveWpfControl(control);
        }

        /// <summary>
        /// Selects the tab which contains the argument control
        /// </summary>
        /// <param name="control"></param>
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

        public System.Drawing.Point GetMousePosition()
        {
            return MainWindow.MousePosition;
        }

        public void HideTools()
        {
            mMainWindow.LeftAndEverythingContainer.Panel1Collapsed = true;
            mMainWindow.VariablesAndEverythingElse.Panel1Collapsed = true;
            mMainWindow.PreviewSplitContainer.Panel2Collapsed = true;
        }

        public void ShowTools()
        {
            mMainWindow.LeftAndEverythingContainer.Panel1Collapsed = false;
            mMainWindow.VariablesAndEverythingElse.Panel1Collapsed = false;
            mMainWindow.PreviewSplitContainer.Panel2Collapsed = false;

        }

        internal void FocusSearch()
        {
            ElementTreeViewManager.Self.FocusSearch();
        }
    }
}
