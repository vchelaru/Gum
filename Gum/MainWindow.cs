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
using Gum.Debug;
using Gum.PropertyGridHelpers;

namespace Gum
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            GumCommands.Self.Initialize(this);

            TypeManager.Self.Initialize();
            PluginManager.Self.Initialize(this);
            
            ElementTreeViewManager.Self.Initialize(this.ObjectTreeView);
            StateTreeViewManager.Self.Initialize(this.StateTreeView, StateContextMenuStrip);
            PropertyGridManager.Self.Initialize(this.VariablePropertyGrid, 
                ((TestWpfControl)this.VariableHost.Child).DataGrid,
                ((TestWpfControl)this.EventsHost.Child).DataGrid
                
                );
            StandardElementsManager.Self.Initialize();
            MenuStripManager.Self.Initialize(RemoveElementMenuItem, RemoveStateMenuItem);
            GuiCommands.Self.Initialize(wireframeControl1);
            Wireframe.WireframeObjectManager.Self.Initialize(WireframeEditControl, wireframeControl1);
            wireframeControl1.XnaInitialize += new Action(HandleXnaInitialize);
            //wireframeControl1.AfterXnaInitialize += HandleXnaInitialize;
            EditingManager.Self.Initialize(this.WireframeContextMenuStrip);
            OutputManager.Self.Initialize(this.OutputTextBox);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.

        }

        //void HandleXnaInitialize(object sender, EventArgs e)
        void HandleXnaInitialize()
        {
            this.wireframeControl1.Initialize(WireframeEditControl);
        }

        private void VariableCenterAndEverythingRight_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoManager.Self.PerformUndo();
        }

        private void screenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddScreenClick();
        }

        private void componentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddComponentClick();
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
            SetVariableLogic.Self.PropertyValueChanged(s, e);
        }

        private void stateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StateTreeViewManager.Self.AddStateClick();
        }

        private void RemoveElementMenuItem_Click(object sender, EventArgs e)
        {
            EditingManager.Self.RemoveSelectedElement();
        }

        private void RemoveStateOrCategoryMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedStateSave != null)
            {
                ObjectRemover.Self.Remove(SelectedState.Self.SelectedStateSave);
            }
            else if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                ObjectRemover.Self.Remove(SelectedState.Self.SelectedStateCategorySave);
            }
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
            ElementTreeViewManager.Self.HandleKeyDown(e);
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
            ProjectManager.Self.RecentFilesUpdated += RefreshRecentFiles;
            ProjectManager.Self.Initialize();

            // Apply FrameRate, but keep it within sane limits
            float FrameRate = Math.Max(Math.Min(ProjectManager.Self.GeneralSettingsFile.FrameRate, 60), 10);
            wireframeControl1.DesiredFramesPerSecond = FrameRate;
        }

        private void VariablesAndEverythingElse_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ObjectFinder.Self.GumProjectSave == null)
            {
                MessageBox.Show("There is no project loaded.  Either load a project or create a new project before saving");
            }
            else
            {
                // Don't do an auto save, force it!
                GumCommands.Self.FileCommands.ForceSaveProject();
            }
        }

        private void wireframeControl1_DragEnter(object sender, DragEventArgs e)
        {
            DragDropManager.Self.HandleFileDragEnter(sender, e);
        }

        private void wireframeControl1_DragDrop(object sender, DragEventArgs e)
        {
            DragDropManager.Self.HandleFileDragDrop(sender, e);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Gum version " + Application.ProductVersion);
        }

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GumCommands.Self.FileCommands.NewProject();
        }

        private void StateTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                StateTreeViewManager.Self.PopulateMenuStrip();
            }
        }

        private void StateTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            StateTreeViewManager.Self.HandleKeyDown(e);
        }

        private void clearFontCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontManager.Self.DeleteFontCacheFolder();
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ObjectFinder.Self.GumProjectSave == null)
            {
                MessageBox.Show("There is no project loaded.  Either load a project or create a new project before saving");
            }
            else
            {
                // Don't do an auto save, force it!
                GumCommands.Self.FileCommands.ForceSaveProject(true);
            }
        }

        private void ObjectTreeView_AfterSelect_1(object sender, TreeViewEventArgs e)
        {
            // If we use AfterClickSelect instead of AfterSelect then
            // we don't get notified when the user selects nothing.
            // Update - we only want to do this if it's null:
            // Otherwise we can't drag drop
            if (ObjectTreeView.SelectedNode == null)
            {
                ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode);
            }
        }

        public void RefreshRecentFiles()
        {
            this.loadRecentToolStripMenuItem.DropDownItems.Clear();

            foreach (var item in ProjectManager.Self.GeneralSettingsFile.RecentProjects)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem();
                menuItem.Text = item;

                this.loadRecentToolStripMenuItem.DropDownItems.Add(menuItem);

                menuItem.Click += delegate
                {
                    ProjectManager.Self.LoadProject(menuItem.Text);
                };
            }
        }

        private void ObjectTreeView_AfterClickSelect(object sender, TreeViewEventArgs e)
        {
            ElementTreeViewManager.Self.OnSelect(ObjectTreeView.SelectedNode);
        }

        public void AddWpfControl(System.Windows.Controls.UserControl control, string tabTitle)
        {
            System.Windows.Forms.Integration.ElementHost wpfHost;
            wpfHost = new System.Windows.Forms.Integration.ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            wpfHost.Child = control;

            System.Windows.Forms.TabPage tabPage = new TabPage();

            tabPage.Controls.Add(wpfHost);
            tabPage.Location = new System.Drawing.Point(4, 22);
            tabPage.Padding = new System.Windows.Forms.Padding(3);
            tabPage.Size = new System.Drawing.Size(230, 463);
            tabPage.TabIndex = 1;
            tabPage.Text = tabTitle;
            tabPage.UseVisualStyleBackColor = true;


            this.PropertyGridTab.Controls.Add(tabPage);
        }

        private void findFileReferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonFormsAndControls.TextInputWindow tiw = new CommonFormsAndControls.TextInputWindow();
            tiw.Message = "Enter entire or partial file name:";
            var dialogResult = tiw.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {

                var elements = ObjectFinder.Self.GetElementsReferencing(tiw.Result);

                string message = "File referenced by:";

                if (elements.Count == 0)
                {
                    message += "\nNothing references this file";
                }
                else
                {
                    foreach (var element in elements)
                    {
                        message += "\n" + element.ToString();
                    }
                }
                MessageBox.Show(message);
            }
        }

        private void ObjectTreeView_MouseMove(object sender, MouseEventArgs e)
        {

            ElementTreeViewManager.Self.HandleMouseOver(e.X, e.Y);
        }
    }
}
