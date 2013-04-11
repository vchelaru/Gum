using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.Managers;
using Gum.ToolStates;
using Gum.ToolCommands;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Wireframe;
using Gum.Gui.Forms;
using Gum.Undo;

namespace Gum
{
    public partial class MainWindow : Form
    {


        public MainWindow()
        {
            InitializeComponent();

            TypeManager.Self.Initialize();
            PluginManager.Self.Initialize(this);
            ElementTreeViewManager.Self.Initialize(this.ObjectTreeView);
            StateTreeViewManager.Self.Initialize(this.StateTreeView);
            PropertyGridManager.Self.Initialize(this.VariablePropertyGrid);
            StandardElementsManager.Self.Initialize();
            MenuStripManager.Self.Initialize(RemoveElementMenuItem, RemoveStateMenuItem);
            GuiCommands.Self.Initialize(wireframeControl1);
            Wireframe.WireframeObjectManager.Self.Initialize(WireframeEditControl);
            wireframeControl1.XnaInitialize += new Action(HandleXnaInitialize);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
        }

        void HandleXnaInitialize()
        {
            this.wireframeControl1.Initialize(WireframeEditControl);
            EditingManager.Self.Initialize(WireframeContextMenuStrip);
        }

        private void VariableCenterAndEverythingRight_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void screenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddScreenClick();
        }

        private void componentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddComponentClick();
        }

        private void ObjectTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode, e.Node);
        }

        private void instanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddInstanceClick();
        }

        private void StateTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            StateTreeViewManager.Self.OnSelect();
        }

        private void loadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool newProjectLoaded = ProjectManager.Self.LoadProject();
        }

        private void VariablePropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            PropertyGridManager.Self.PropertyValueChanged(s, e);
        }

        private void stateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StateTreeViewManager.Self.AddStateClick();
        }

        private void RemoveElementMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.RemoveSelectedElement();
        }

        private void RemoveStateMenuItem_Click(object sender, EventArgs e)
        {
            ElementCommands.Self.RemoveState(SelectedState.Self.SelectedStateSave, SelectedState.Self.SelectedElement);
        }

        private void ObjectTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ElementTreeViewManager.Self.PopulateMenuStrip();
            }
        }

        private void wireframeControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                EditingManager.Self.OnRightClick();
            }
        }

        private void ObjectTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            this.wireframeControl1.HandleCopyCutPaste(e);

            this.wireframeControl1.HandleDelete(e);
        }

        private void ObjectTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DragDropManager.Self.OnItemDrag(e.Item);
        }

        private void projectPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectPropertyGridWindow ppgw = new ProjectPropertyGridWindow();

            ppgw.Show(this);

            ppgw.Location = new Point(MainWindow.MousePosition.X, MainWindow.MousePosition.Y);
        }

        private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PluginsWindow pluginsWindow = new PluginsWindow();
            pluginsWindow.Show();
        }

        private void PropertyGridMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            PropertyGridManager.Self.OnPropertyGridRightClick(sender as ContextMenuStrip);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ProjectManager.Self.Initialize();

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // CTRL+Z, control z, undo, ctrl z, ctrl + z
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    UndoManager.Self.PerformUndo();
                    return true;
                default:
                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void VariablesAndEverythingElse_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectManager.Self.SaveProject();
        }
    }
}
