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

            this.GuideListDisplay.GumProjectSave = ProjectState.Self.GumProjectSave;

            mDisplayer = new GumProjectSavePropertyGridDisplayer();
            mDisplayer.GumProjectSave = ProjectState.Self.GumProjectSave;
            this.propertyGrid1.SelectedObject = mDisplayer;

            this.GuideListDisplay.PropertyGridChanged += new EventHandler(OnPropertyGridChanged);
            this.GuideListDisplay.NewGuideAdded += new EventHandler(OnNewGuideAdded);

        }

        void OnNewGuideAdded(object sender, EventArgs e)
        {
            ProjectCommands.Self.SaveProject();
            WireframeObjectManager.Self.UpdateGuides();
        }

        void OnPropertyGridChanged(object sender, EventArgs e)
        {
            ProjectCommands.Self.SaveProject();
            WireframeObjectManager.Self.UpdateGuides();
            WireframeObjectManager.Self.RefreshAll(true);
            PluginManager.Self.GuidesChanged();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {

            ProjectCommands.Self.SaveProject();

            GuiCommands.Self.RefreshWireframeDisplay();
        }
    }
}
