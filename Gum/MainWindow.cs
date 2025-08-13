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
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Controls;
using Gum.Logic.FileWatch;
using Gum.DataTypes;
using Gum.Services;
using Gum.Undo;
using Gum.Logic;
using Gum.Plugins.InternalPlugins.MenuStripPlugin;
using Gum.Services.Dialogs;
using GumRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    public partial class MainWindow : Form, IRecipient<CloseMainWindowMessage>
    {
        #region Fields/Properties

        private readonly IGuiCommands _guiCommands;
        
        private System.Windows.Forms.Timer FileWatchTimer;

        MainPanelControl mainPanelControl;

        #endregion

        public MainWindow(MainPanelControl mainPanelControl,
            IGuiCommands guiCommands,
            MenuStripManager menuStripManager,
            IMessenger messenger
            )
        {
#if DEBUG
        // This suppresses annoying, useless output from WPF, as explained here:
        http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Critical;
#endif
            messenger.RegisterAll(this);
            
            InitializeComponent();

            AddMainPanelControl(mainPanelControl);

            this.KeyPreview = true;
            this.KeyDown += HandleKeyDown;

            // Initialize before the StateView is created...
            _guiCommands = guiCommands;
            _guiCommands.Initialize(mainPanelControl);

            TypeManager.Self.Initialize();

            // This has to happen before plugins are loaded since they may depend on settings...
            ProjectManager.Self.LoadSettings();

            Cursor addCursor = LoadAddCursor();
            _guiCommands.AddCursor = addCursor;
            // Vic says - I tried
            // to instantiate the ElementTreeImages
            // in the ElementTreeViewManager. I move 
            // the code there and it works, but then at
            // some point it stops working and it breaks. Not 
            // sure why, Winforms editor must be doing something
            // beyond the generation of code which isn't working when
            // I move it to custom code. Oh well, maybe one day I'll move
            // to a wpf window and can get rid of this
            // For Vic K: This should die. We won't need it once we move to
            // a WPF treeview. 
            ElementTreeViewManager.Self.Initialize(this.components, ElementTreeImages);

            // ProperGridManager before MenuStripManager. Why does it need to be initialized before MainMenuStripPlugin?
            // Is htere a way to move this to a plugin?
            PropertyGridManager.Self.InitializeEarly();

            // bah we have to do this before initializing all plugins because we need the menu strip to exist:
            this.Controls.Add(MainMenuStrip = menuStripManager.CreateMenuStrip());

            PluginManager.Self.Initialize(this);

            StandardElementsManager.Self.Initialize();
            StandardElementsManager.Self.CustomGetDefaultState =
                PluginManager.Self.GetDefaultStateFor;

            ElementSaveExtensions.VariableChangedThroughReference +=
                Gum.Plugins.PluginManager.Self.VariableSet;


            StandardElementsManagerGumTool.Self.Initialize();

            VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;


            // ProjectManager.Initialize used to happen here, but I 
            // moved it down to the Load event for MainWindow because
            // ProjectManager.Initialize may load a project, and if it
            // does, then we need to make sure that the wireframe controls
            // are set up properly before that happens.
            
            WireframeObjectManager.Self.Initialize();

            PluginManager.Self.XnaInitialized();

            InitializeFileWatchTimer();
        }

        private Cursor LoadAddCursor()
        {
            try
            {
                var cursor = new System.Windows.Forms.Cursor(this.GetType(), "Content.Cursors.AddCursor.cur");
                return cursor;
            }
            catch
            {
                // Vic got this to crash on Sean's machine. Not sure why, but let's tolerate it since it's not breaking
                return Cursor.Current;
            }
        }

        private void AddMainPanelControl(MainPanelControl mainPanelControl)
        {
            var wpfHost = new ElementHost();
            wpfHost.Dock = DockStyle.Fill;
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
                _guiCommands.FocusSearch();
                args.Handled = true;
                args.SuppressKeyPress = true;
            }
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

        private async void MainWindow_Load(object sender, EventArgs e)
        {
            await ProjectManager.Self.Initialize();

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
            else
            {
                Close();
            }
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            var settings = ProjectManager.Self.GeneralSettingsFile;

            settings.MainWindowBounds = DesktopBounds;
            settings.MainWindowState = WindowState;

            settings.Save();
        }

        void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message)
        {
            Close();
        }
    }
}
