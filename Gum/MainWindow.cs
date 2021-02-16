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
using Gum.Logic.FileWatch;
using Gum.Commands;

namespace Gum
{
    public enum TabLocation
    {
        [Obsolete("Use either CenterTop or CenterBottom")]
        Center,
        RightBottom,
        RightTop,
        CenterTop, 
        CenterBottom,
        Left
    }

    public partial class MainWindow : Form
    {
        private System.Windows.Forms.Timer FileWatchTimer;
        private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl WireframeEditControl;
        private Wireframe.WireframeControl wireframeControl1;
        ScrollBarControlLogic scrollBarControlLogic;
        public System.Windows.Forms.FlowLayoutPanel ToolbarPanel;
        Panel gumEditorPanel;


        StateView stateView;

        public MainWindow()
        {
#if DEBUG
        // This suppresses annoying, useless output from WPF, as explained here:
        http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif


            InitializeComponent();



            // Create the wireframe control, but don't add it...
            CreateWireframeControl();

            gumEditorPanel = new Panel();

            // place the scrollbars first so they are in front of everything
            scrollBarControlLogic = new ScrollBarControlLogic(gumEditorPanel, wireframeControl1);
            scrollBarControlLogic.SetDisplayedArea(800, 600);
            wireframeControl1.CameraChanged += () =>
            {
                if(ProjectManager.Self.GumProjectSave != null)
                {
                    
                    scrollBarControlLogic.SetDisplayedArea(
                        ProjectManager.Self.GumProjectSave.DefaultCanvasWidth,
                        ProjectManager.Self.GumProjectSave.DefaultCanvasHeight);
                }
                else
                {
                    scrollBarControlLogic.SetDisplayedArea(800, 600);
                }

                scrollBarControlLogic.UpdateScrollBars();
                scrollBarControlLogic.UpdateScrollBarsToCameraPosition();

            };


            //... add it here, so it can be done after scroll bars and other controls
            gumEditorPanel.Controls.Add(this.wireframeControl1);

            CreateWireframeEditControl();
            CreateToolbarPanel();


            stateView = new StateView();
            this.AddWinformsControl(stateView, "States", TabLocation.CenterTop);

            ((SelectedState)SelectedState.Self).Initialize(stateView);
            GumCommands.Self.Initialize(this);
            GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);

            TypeManager.Self.Initialize();

            // Vic says - I tried
            // to instantiate the ElementTreeImages
            // in the ElementTreeViewManager. I move 
            // the code there and it works, but then at
            // some point it stops working and it breaks. Not 
            // sure why, Winforms editor must be doing something
            // beyond the generation of code which isn't working when
            // I move it to custom code. Oh well, maybe one day I'll move
            // to a wpf window and can get rid of this
            ElementTreeViewManager.Self.Initialize(this.components, ElementTreeImages);
            // State Tree ViewManager needs init before MenuStripManager
            StateTreeViewManager.Self.Initialize(this.stateView.TreeView, this.stateView.StateContextMenuStrip);
            // ProperGridManager before MenuSTripManager
            PropertyGridManager.Self.Initialize();
            // menu strip manager needs to be initialized before plugins:
            MenuStripManager.Self.Initialize(this);

            PluginManager.Self.Initialize(this);

            StandardElementsManager.Self.Initialize();

            
            ToolCommands.GuiCommands.Self.Initialize(wireframeControl1);
            Wireframe.WireframeObjectManager.Self.Initialize(WireframeEditControl, wireframeControl1);

            wireframeControl1.XnaUpdate += () =>
                Wireframe.WireframeObjectManager.Self.Activity();


            EditingManager.Self.Initialize(this.WireframeContextMenuStrip);
            OutputManager.Self.Initialize(this.OutputTextBox);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            HandleXnaInitialize();

            InitializeFileWatchTimer();

        }

        private void CreateToolbarPanel()
        {
            this.ToolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
            gumEditorPanel.Controls.Add(this.ToolbarPanel);
            // 
            // ToolbarPanel
            // 
            //this.ToolbarPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            //| System.Windows.Forms.AnchorStyles.Right)));
            this.ToolbarPanel.Dock = DockStyle.Top;
            this.ToolbarPanel.Location = new System.Drawing.Point(0, 22);
            this.ToolbarPanel.Name = "ToolbarPanel";
            this.ToolbarPanel.Size = new System.Drawing.Size(532, 31);
            this.ToolbarPanel.TabIndex = 2;
        }

        private void CreateWireframeControl()
        {
            this.wireframeControl1 = new Gum.Wireframe.WireframeControl();
            // 
            // wireframeControl1
            // 
            this.wireframeControl1.AllowDrop = true;
            //this.wireframeControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            //| System.Windows.Forms.AnchorStyles.Left)
            //| System.Windows.Forms.AnchorStyles.Right)));
            this.wireframeControl1.Dock = DockStyle.Fill;
            this.wireframeControl1.ContextMenuStrip = this.WireframeContextMenuStrip;
            this.wireframeControl1.Cursor = System.Windows.Forms.Cursors.Default;
            this.wireframeControl1.DesiredFramesPerSecond = 30F;
            this.wireframeControl1.Location = new System.Drawing.Point(0, 52);
            this.wireframeControl1.Name = "wireframeControl1";
            this.wireframeControl1.Size = new System.Drawing.Size(532, 452);
            this.wireframeControl1.TabIndex = 0;
            this.wireframeControl1.Text = "wireframeControl1";

            this.wireframeControl1.DragDrop += new System.Windows.Forms.DragEventHandler(this.wireframeControl1_DragDrop);
            this.wireframeControl1.DragEnter += new System.Windows.Forms.DragEventHandler(this.wireframeControl1_DragEnter);
            this.wireframeControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.wireframeControl1_MouseClick);


        }

        private void CreateWireframeEditControl()
        {
            this.WireframeEditControl = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl();
            gumEditorPanel.Controls.Add(this.WireframeEditControl);
            // 
            // WireframeEditControl
            // 
            //this.WireframeEditControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            //| System.Windows.Forms.AnchorStyles.Right)));
            this.WireframeEditControl.Dock = DockStyle.Top;
            this.WireframeEditControl.Location = new System.Drawing.Point(0, 0);
            this.WireframeEditControl.Margin = new System.Windows.Forms.Padding(4);
            this.WireframeEditControl.Name = "WireframeEditControl";
            this.WireframeEditControl.PercentageValue = 100;
            this.WireframeEditControl.TabIndex = 1;
        }

        private void InitializeFileWatchTimer()
        {
            this.FileWatchTimer = new Timer(this.components);
            this.FileWatchTimer.Enabled = true;
            this.FileWatchTimer.Interval = 1000;
            this.FileWatchTimer.Tick += new System.EventHandler(HandleFileWatchTimer);
        }

        private void HandleFileWatchTimer(object sender, EventArgs e)
        {
            var gumProject = ProjectState.Self.GumProjectSave;
            if (gumProject != null && !string.IsNullOrEmpty(gumProject.FullFileName))
            {

                FileWatchManager.Self.Flush();
            }
        }

        //void HandleXnaInitialize(object sender, EventArgs e)
        void HandleXnaInitialize()
        {
            this.wireframeControl1.Initialize(WireframeEditControl, gumEditorPanel);
            scrollBarControlLogic.Managers = global::RenderingLibrary.SystemManagers.Default;
            scrollBarControlLogic.UpdateScrollBars();

            this.wireframeControl1.Parent.Resize += (not, used) =>
            {
                UpdateWireframeControlSizes();
                scrollBarControlLogic.UpdateScrollBars();
            };

            UpdateWireframeControlSizes();
        }

        /// <summary>
        /// Refreshes the wifreframe control size - for some reason this is necessary if windows has a non-100% scale (for higher resolution displays)
        /// </summary>
        private void UpdateWireframeControlSizes()
        {
            // I don't think we need this for docking:
            //WireframeEditControl.Width = WireframeEditControl.Parent.Width / 2;

            ToolbarPanel.Width = ToolbarPanel.Parent.Width;

            wireframeControl1.Width = wireframeControl1.Parent.Width;

            // Add location.Y to account for the shortcut bar at the top.
            wireframeControl1.Height = wireframeControl1.Parent.Height - wireframeControl1.Location.Y;
        }

        private void VariableCenterAndEverythingRight_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void VariablePropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            SetVariableLogic.Self.PropertyValueChanged(s, e);
        }

        private void wireframeControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                EditingManager.Self.OnRightClick();
            }
        }

        private void PropertyGridMenuStrip_Opening(object sender, CancelEventArgs e)
        {
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ProjectManager.Self.RecentFilesUpdated += MenuStripManager.Self.RefreshRecentFilesMenuItems;
            ProjectManager.Self.Initialize();

            if(CommandLine.CommandLineManager.Self.ShouldExitImmediately == false)
            {

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

        private void wireframeControl1_DragEnter(object sender, DragEventArgs e)
        {
            DragDropManager.Self.HandleFileDragEnter(sender, e);
        }

        private void wireframeControl1_DragDrop(object sender, DragEventArgs e)
        {
            DragDropManager.Self.HandleFileDragDrop(sender, e);
        }

        public TabPage AddWinformsControl(Control control, string tabTitle, TabLocation tabLocation)
        {
            // todo: check if control has already been added. Right now this can't be done trough the Gum commands
            // so it's only used "internally", so no checking is being done.
            var tabControl = GetTabFromLocation(tabLocation);
            var tabPage = CreateTabPage(tabTitle);
            control.Dock = DockStyle.Fill;
            tabControl.Controls.Add(tabPage);

            tabPage.Controls.Add(control);

            return tabPage;
        }

        public TabPage AddWpfControl(System.Windows.Controls.UserControl control, string tabTitle, TabLocation tabLocation = TabLocation.Center)
        {
            TabPage existingTabPage;
            TabControl existingTabControl;
            GetContainers(control, out existingTabPage, out existingTabControl);

            bool alreadyExists = existingTabControl != null;

            TabPage tabPage = existingTabPage;

            if (!alreadyExists)
            {

                System.Windows.Forms.Integration.ElementHost wpfHost;
                wpfHost = new System.Windows.Forms.Integration.ElementHost();
                wpfHost.Dock = DockStyle.Fill;
                wpfHost.Child = control;

                tabPage = CreateTabPage(tabTitle);

                TabControl tabControl = GetTabFromLocation(tabLocation);
                tabControl.Controls.Add(tabPage);

                tabPage.Controls.Add(wpfHost);

            }

            return tabPage;
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
                case TabLocation.RightBottom:
                    tabControl = this.RightBottomTabControl;

                    break;
                case TabLocation.RightTop:
                    tabControl = this.RightTopTabControl;
                    break;
                case TabLocation.CenterTop:
                    tabControl = this.tabControl1;
                    break;
                case TabLocation.Left:
                    tabControl = this.LeftTabControl;
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
                foreach (var uncastedTabPage in this.RightBottomTabControl.Controls)
                {
                    tabPage = uncastedTabPage as TabPage;

                    if (tabPage != null && DoesTabContainControl(tabPage, control))
                    {
                        tabControl = this.RightBottomTabControl;
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

    }
}
