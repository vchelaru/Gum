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
using Gum.DataTypes;
using Gum.Services;
using Gum.Undo;
using Gum.Logic;
using Gum.Plugins.InternalPlugins.EditorTab;

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
        public System.Windows.Forms.FlowLayoutPanel ToolbarPanel;
        private Wireframe.WireframeControl wireframeControl1;
        Panel gumEditorPanel;

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
            var builder = new Builder();
            builder.Build();

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

            TypeManager.Self.Initialize();

            // This has to happen before plugins are loaded since they may depend on settings...
            ProjectManager.Self.LoadSettings();


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
            ElementTreeViewManager.Self.Initialize(this.components, ElementTreeImages, addCursor, CopyPasteLogic.Self);

            // ProperGridManager before MenuStripManager
            PropertyGridManager.Self.InitializeEarly();
            // menu strip manager needs to be initialized before plugins:

            var menuStripManager = new MenuStripManager();
            menuStripManager.Initialize(this);

            UndoManager.Self.Initialize(menuStripManager);

            ((SelectedState)SelectedState.Self).Initialize(menuStripManager);

            PluginManager.Self.Initialize(this);

            GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
            GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;

            GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
            GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;

            // do this after initializing the plugins. This is separate from where the initialize call is made, but it must
            // happen after plugins are created:
            MainEditorTabPlugin.Self.HandleWireframeInitialized(wireframeControl1, WireframeEditControl, 
                addCursor, gumEditorPanel, ToolbarPanel);

            StandardElementsManager.Self.Initialize();
            StandardElementsManager.Self.CustomGetDefaultState = 
                PluginManager.Self.GetDefaultStateFor;

            StandardElementsManagerGumTool.Self.Initialize();

            VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;


            EditingManager.Self.Initialize(this.WireframeContextMenuStrip);
            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            PluginManager.Self.XnaInitialized();


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

        // todo - a lot of this has moved to MainEditorTabPlugin. Need to finish that migration...
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

            gumEditorPanel = new Panel();


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

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ProjectManager.Self.Initialize();

            if(CommandLine.CommandLineManager.Self.ShouldExitImmediately == false)
            {
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
            }
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            var settings = ProjectManager.Self.GeneralSettingsFile;

            settings.MainWindowBounds = DesktopBounds;
            settings.MainWindowState = WindowState;

            settings.Save();
        }
    }
}
