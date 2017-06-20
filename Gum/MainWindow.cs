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
using System.Windows.Forms.Integration;
using Gum.DataTypes;
using Gum.Controls;

namespace Gum
{
    public enum TabLocation
    {
        [Obsolete("Use either CenterTop or CenterBottom")]
        Center,
        Right,
        CenterTop, 
        CenterBottom
    }

    public partial class MainWindow : Form
    {
        StateView stateView;

        public MainWindow()
        {
#if DEBUG
            // This suppresses annoying, useless output from WPF, as explained here:
            http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif


            InitializeComponent();

            stateView = new StateView();
            this.AddWinformsControl(stateView, "States", TabLocation.CenterTop);

            ((SelectedState)SelectedState.Self).Initialize(stateView);
            GumCommands.Self.Initialize(this);

            TypeManager.Self.Initialize();
            PluginManager.Self.Initialize(this);
            
            ElementTreeViewManager.Self.Initialize(this.ObjectTreeView);
            StateTreeViewManager.Self.Initialize(this.stateView.TreeView, this.stateView.StateContextMenuStrip);
            PropertyGridManager.Self.Initialize( 
                ((TestWpfControl)this.VariableHost.Child),
                ((TestWpfControl)this.EventsHost.Child).DataGrid
                
                );
            StandardElementsManager.Self.Initialize();
            MenuStripManager.Self.Initialize(
                RemoveElementMenuItem, RemoveStateMenuItem, RemoveVariableMenuItem);
            GuiCommands.Self.Initialize(wireframeControl1);
            Wireframe.WireframeObjectManager.Self.Initialize(WireframeEditControl, wireframeControl1);

            EditingManager.Self.Initialize(this.WireframeContextMenuStrip);
            OutputManager.Self.Initialize(this.OutputTextBox);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            HandleXnaInitialize();
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
            ElementTreeViewManager.Self.AddScreenClick(sender, e);
        }

        private void componentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddComponentClick(sender, e);
        }

        private void instanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ElementTreeViewManager.Self.AddInstanceClick(sender, e);
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

        private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PluginsWindow pluginsWindow = new PluginsWindow();
            pluginsWindow.Show();
        }

        private void PropertyGridMenuStrip_Opening(object sender, CancelEventArgs e)
        {
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ProjectManager.Self.RecentFilesUpdated += RefreshRecentFiles;
            ProjectManager.Self.Initialize();

            // Apply FrameRate, but keep it within sane limits
            float FrameRate = Math.Max(Math.Min(ProjectManager.Self.GeneralSettingsFile.FrameRate, 60), 10);
            wireframeControl1.DesiredFramesPerSecond = FrameRate;

            var settings = ProjectManager.Self.GeneralSettingsFile;

            // Apply the window position and size settings only if a large enough portion of the
            // window would end up on the screen.
            var workingArea = Screen.GetWorkingArea(settings.MainWindowBounds);
            var intersection = Rectangle.Intersect(settings.MainWindowBounds, workingArea);
            if (intersection.Width > 100 && intersection.Height > 100)
            {
                DesktopBounds = settings.MainWindowBounds;
                WindowState = settings.MainWindowState;
            }

            LeftAndEverythingContainer.SplitterDistance
                = Math.Max(0, settings.LeftAndEverythingSplitterDistance);
            PreviewSplitContainer.SplitterDistance
                = Math.Max(0, settings.PreviewSplitterDistance);
            StatesAndVariablesContainer.SplitterDistance
                = Math.Max(0, settings.StatesAndVariablesSplitterDistance);
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            var settings = ProjectManager.Self.GeneralSettingsFile;

            settings.MainWindowBounds = DesktopBounds;
            settings.MainWindowState = WindowState;

            settings.LeftAndEverythingSplitterDistance
                = LeftAndEverythingContainer.SplitterDistance;
            settings.PreviewSplitterDistance
                = PreviewSplitContainer.SplitterDistance;
            settings.StatesAndVariablesSplitterDistance
                = StatesAndVariablesContainer.SplitterDistance;

            settings.Save();
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

            foreach (var item in ProjectManager.Self.GeneralSettingsFile.RecentProjects.OrderByDescending(item=>item.LastTimeOpened))
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem();
                menuItem.Text = item.AbsoluteFileName;

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

        public void AddWinformsControl(Control control, string tabTitle, TabLocation tabLocation)
        {
            // todo: check if control has already been added. Right now this can't be done trough the Gum commands
            // so it's only used "internally", so no checking is being done.
            var tabControl = GetTabFromLocation(tabLocation);
            var tabPage = CreateTabPage(tabTitle);
            control.Dock = DockStyle.Fill;
            tabControl.Controls.Add(tabPage);

            tabPage.Controls.Add(control);
        }

        public void AddWpfControl(System.Windows.Controls.UserControl control, string tabTitle, TabLocation tabLocation = TabLocation.Center)
        {
            TabPage existingTabPage;
            TabControl existingTabControl;
            GetContainers(control, out existingTabPage, out existingTabControl);

            bool alreadyExists = existingTabControl != null;

            if (!alreadyExists)
            {

                System.Windows.Forms.Integration.ElementHost wpfHost;
                wpfHost = new System.Windows.Forms.Integration.ElementHost();
                wpfHost.Dock = DockStyle.Fill;
                wpfHost.Child = control;

                TabPage tabPage = CreateTabPage(tabTitle);

                TabControl tabControl = GetTabFromLocation(tabLocation);
                tabControl.Controls.Add(tabPage);

                tabPage.Controls.Add(wpfHost);

            }
        }

        private static TabPage CreateTabPage(string tabTitle)
        {
            System.Windows.Forms.TabPage tabPage = new TabPage();
            tabPage.Location = new System.Drawing.Point(4, 22);
            tabPage.Padding = new System.Windows.Forms.Padding(3);
            tabPage.Size = new System.Drawing.Size(230, 463);
            tabPage.TabIndex = 1;
            tabPage.Text = tabTitle;
            tabPage.UseVisualStyleBackColor = true;
            return tabPage;
        }

        private TabControl GetTabFromLocation(TabLocation tabLocation)
        {
            TabControl tabControl = null;

            switch (tabLocation)
            {
                case TabLocation.Center:
                case TabLocation.CenterBottom:
                    tabControl = this.MiddleTabControl;
                    break;
                case TabLocation.Right:
                    tabControl = this.RightTabControl;

                    break;
                case TabLocation.CenterTop:
                    tabControl = this.tabControl1;
                    break;
                default:
                    throw new NotImplementedException($"Tab location {tabLocation} not supported");
            }

            return tabControl;
        }

        private void GetContainers(System.Windows.Controls.UserControl control, out TabPage tabPage, out TabControl tabControl)
        {
            tabPage = null;
            tabControl = null;

            foreach (var uncastedTabPage in this.MiddleTabControl.Controls)
            {
                tabPage = uncastedTabPage as TabPage;

                if (tabPage != null && DoesTabContainControl(tabPage, control))
                {
                    tabControl = this.MiddleTabControl;

                    break;
                }
                else
                {
                    tabPage = null;
                }
            }

            if (tabControl == null)
            {
                foreach (var uncastedTabPage in this.RightTabControl.Controls)
                {
                    tabPage = uncastedTabPage as TabPage;

                    if (tabPage != null && DoesTabContainControl(tabPage, control))
                    {
                        tabControl = this.RightTabControl;
                        break;
                    }
                    else
                    {
                        tabPage = null;
                    }
                }
            }
        }


        internal void ShowTabForControl(System.Windows.Controls.UserControl control)
        {
            TabControl tabControl = null;
            TabPage tabPage = null;
            GetContainers(control, out tabPage, out tabControl);

            var index = tabControl.TabPages.IndexOf(tabPage);

            tabControl.SelectedIndex = index;
        }


        public void RemoveWpfControl(System.Windows.Controls.UserControl control)
        {
            List<Control> controls = new List<Control>();

            TabControl tabControl = null;
            TabPage tabPage = null;
            GetContainers(control, out tabPage, out tabControl);
            
            if(tabControl != null)
            {
                foreach(var controlInTabPage in tabPage.Controls)
                {
                    if(controlInTabPage is ElementHost)
                    {
                        (controlInTabPage as ElementHost).Child = null;
                    }
                }
                tabPage.Controls.Clear();
                tabControl.Controls.Remove(tabPage);
            }
        }

        bool DoesTabContainControl(TabPage tabPage, System.Windows.Controls.UserControl control)
        {
            var foundHost = tabPage.Controls
                .FirstOrDefault(item => item is System.Windows.Forms.Integration.ElementHost)
                as System.Windows.Forms.Integration.ElementHost;

            return foundHost != null && foundHost.Child == control;
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
