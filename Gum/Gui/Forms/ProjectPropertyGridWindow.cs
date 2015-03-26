using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Plugins;

namespace Gum.Gui.Forms
{
    public partial class ProjectPropertyGridWindow : Form
    {
        GumProjectSavePropertyGridDisplayer mDisplayer = new GumProjectSavePropertyGridDisplayer();

        public ProjectPropertyGridWindow()
        {
            InitializeComponent();

            AutoSaveCheckBox.Checked = ProjectManager.Self.GeneralSettingsFile.AutoSave;

            this.GuideListDisplay.GumProjectSave = ProjectState.Self.GumProjectSave;

            mDisplayer = new GumProjectSavePropertyGridDisplayer();
            mDisplayer.GumProjectSave = ProjectState.Self.GumProjectSave;
            mDisplayer.GeneralSettings = ProjectManager.Self.GeneralSettingsFile;

            this.TopPropertyGrid.SelectedObject = mDisplayer;

            this.GuideListDisplay.PropertyGridChanged += OnGuidePropertyGridChanged;
            this.GuideListDisplay.NewGuideAdded += OnNewGuideAdded;
        }

        void OnNewGuideAdded(object sender, EventArgs e)
        {
            GumCommands.Self.FileCommands.TryAutoSaveProject();
            WireframeObjectManager.Self.UpdateGuides();
        }

        void OnGuidePropertyGridChanged(object sender, EventArgs e)
        {
            GumCommands.Self.FileCommands.TryAutoSaveProject();

            WireframeObjectManager.Self.UpdateGuides();
            WireframeObjectManager.Self.RefreshAll(true);
            PluginManager.Self.GuidesChanged();
            
            //EditingManager.Self.UpdateSelectedObjectsPositionAndDimensions();
        }

        private void TopPropertyGridValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Set the canvas width/height first before refreshing everyting so that the refresh uses these values:
            GraphicalUiElement.CanvasWidth = ProjectState.Self.GumProjectSave.DefaultCanvasWidth;
            GraphicalUiElement.CanvasHeight = ProjectState.Self.GumProjectSave.DefaultCanvasHeight;

            if (ProjectState.Self.GumProjectSave != null)
            {
                GraphicalUiElement.ShowLineRectangles = ProjectState.Self.GumProjectSave.ShowOutlines;
                EditingManager.Self.RestrictToUnitValues = ProjectState.Self.GumProjectSave.RestrictToUnitValues;
            }

            //I think we need to refresh *after* we show line rectangles
            GuiCommands.Self.UpdateWireframeToProject();
            WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveProject();

            //EditingManager.Self.UpdateSelectedObjectsPositionAndDimensions();
        }

        private void AutoSaveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ProjectManager.Self.GeneralSettingsFile.AutoSave = AutoSaveCheckBox.Checked;
        }

        private void ProjectPropertyGridWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProjectManager.Self.GeneralSettingsFile.Save();
        }
    }
}
