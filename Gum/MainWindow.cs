using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Wireframe;
using Gum.PropertyGridHelpers;
using System.Windows.Forms.Integration;
using Gum.Controls;
using Gum.Logic.FileWatch;
using Microsoft.AppCenter.Crashes;
using Gum.DataTypes;

namespace Gum
{
    #region TabLocation Enum
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
    #endregion

    public partial class MainWindow : Form
    {
        #region Fields/Properties

        private System.Windows.Forms.Timer FileWatchTimer;
        private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl WireframeEditControl;
        private Wireframe.WireframeControl wireframeControl1;
        public System.Windows.Forms.FlowLayoutPanel ToolbarPanel;
        Panel gumEditorPanel;
        StateView stateView;

        MainPanelControl mainPanelControl;

        #endregion

        public MainWindow()
        {
#if DEBUG
        // This suppresses annoying, useless output from WPF, as explained here:
        http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Critical;
#endif

            InitializeComponent();

            CreateMainWpfPanel();

            this.KeyPreview = true;
            this.KeyDown += HandleKeyDown;

            // Create the wireframe control, but don't add it...
            CreateWireframeControl();

            CreateWireframeEditControl();
            CreateEditorToolbarPanel();

            // Initialize before the StateView is created...
            GumCommands.Self.Initialize(this, mainPanelControl);

            stateView = new StateView();
            GumCommands.Self.GuiCommands.AddControl(stateView, "States", TabLocation.CenterTop);

            ((SelectedState)SelectedState.Self).Initialize(stateView);
            GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);

            TypeManager.Self.Initialize();

            var addCursor = new System.Windows.Forms.Cursor(this.GetType(), "Content.Cursors.AddCursor.cur");
            // Vic says - I tried
            // to instantiate the ElementTreeImages
            // in the ElementTreeViewManager. I move 
            // the code there and it works, but then at
            // some point it stops working and it breaks. Not 
            // sure why, Winforms editor must be doing something
            // beyond the generation of code which isn't working when
            // I move it to custom code. Oh well, maybe one day I'll move
            // to a wpf window and can get rid of this
            ElementTreeViewManager.Self.Initialize(this.components, ElementTreeImages, addCursor);
            // State Tree ViewManager needs init before MenuStripManager
            StateTreeViewManager.Self.Initialize(this.stateView.TreeView, this.stateView.StateContextMenuStrip);
            // ProperGridManager before MenuStripManager
            PropertyGridManager.Self.InitializeEarly();
            // menu strip manager needs to be initialized before plugins:
            MenuStripManager.Self.Initialize(this);

            PluginManager.Self.Initialize(this);

            // do this after initializing the plugins. This is separate from where the initialize call is made, but it must
            // happen after plugins are created:
            PluginManager.Self.WireframeInitialized(wireframeControl1, gumEditorPanel);


            StandardElementsManager.Self.Initialize();
            StandardElementsManager.Self.CustomGetDefaultState = 
                PluginManager.Self.GetDefaultStateFor;

            StandardElementsManagerGumTool.Self.Initialize();


            ToolCommands.GuiCommands.Self.Initialize(wireframeControl1);


            Wireframe.WireframeObjectManager.Self.Initialize(WireframeEditControl, wireframeControl1, addCursor);

            VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;

            wireframeControl1.XnaUpdate += () =>
                Wireframe.WireframeObjectManager.Self.Activity();


            EditingManager.Self.Initialize(this.WireframeContextMenuStrip);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            HandleXnaInitialize();

            InitializeFileWatchTimer();


        }

        private void CreateMainWpfPanel()
        {
            var wpfHost = new ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            mainPanelControl = new MainPanelControl();
            wpfHost.Child = mainPanelControl;
            this.Controls.Add(wpfHost);
            this.PerformLayout();
        }

        private void HandleKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.F
                 && (args.Modifiers & Keys.Control) == Keys.Control
                )
            {
                GumCommands.Self.GuiCommands.FocusSearch();
                args.Handled = true;
                args.SuppressKeyPress = true;
            }
        }

        private void CreateEditorToolbarPanel()
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

            this.wireframeControl1.DragDrop += DragDropManager.Self.HandleFileDragDrop;
            this.wireframeControl1.DragEnter += DragDropManager.Self.HandleFileDragEnter;
            this.wireframeControl1.DragOver += (sender, e) =>
            {
                //this.DoDragDrop(e.Data, DragDropEffects.Move | DragDropEffects.Copy);
                //DragDropManager.Self.HandleDragOver(sender, e);

            };

                
            wireframeControl1.ErrorOccurred += (exception) => Crashes.TrackError(exception);

            this.wireframeControl1.QueryContinueDrag += (sender, args) =>
            {
                args.Action = DragAction.Continue;
            };

            this.wireframeControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.wireframeControl1_MouseClick);

            this.wireframeControl1.KeyDown += (o, args) =>
            {
                if(args.KeyCode == Keys.Tab)
                {
                    GumCommands.Self.GuiCommands.ToggleToolVisibility();
                }
            };

            gumEditorPanel = new Panel();

            // place the scrollbars first so they are in front of everything
            // Update October 6, 2021
            // The scrollbars have been moved
            // to a plugin. So now they are not 
            // added in the same order. This seems
            // to be okay...

            wireframeControl1.CameraChanged += () =>
            {
                PluginManager.Self.CameraChanged();
            };

            //... add it here, so it can be done after scroll bars and other controls
            gumEditorPanel.Controls.Add(this.wireframeControl1);
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
            PluginManager.Self.XnaInitialized();


            this.wireframeControl1.Parent.Resize += (not, used) =>
            {
                UpdateWireframeControlSizes();
                PluginManager.Self.HandleWireframeResized();
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

                //LeftAndEverythingContainer.SplitterDistance
                //    = Math.Max(0, settings.LeftAndEverythingSplitterDistance);
                //PreviewSplitContainer.SplitterDistance
                //    = Math.Max(0, settings.PreviewSplitterDistance);
                //StatesAndVariablesContainer.SplitterDistance
                //    = Math.Max(0, settings.StatesAndVariablesSplitterDistance);
            }
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            var settings = ProjectManager.Self.GeneralSettingsFile;

            settings.MainWindowBounds = DesktopBounds;
            settings.MainWindowState = WindowState;

            //settings.LeftAndEverythingSplitterDistance
            //    = LeftAndEverythingContainer.SplitterDistance;
            //settings.PreviewSplitterDistance
            //    = PreviewSplitContainer.SplitterDistance;
            //settings.StatesAndVariablesSplitterDistance
            //    = StatesAndVariablesContainer.SplitterDistance;

            settings.Save();
        }

        private void VariablesAndEverythingElse_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }
    }
}
