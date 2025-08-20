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
using Gum.ViewModels;
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
        

        MainPanelControl mainPanelControl;

        #endregion

        public MainWindow(MainPanelControl mainPanelControl,
            MenuStripManager menuStripManager,
            IMessenger messenger,
            PeriodicUiTimer periodicUiTimer,
            MainWindowViewModel mainWindowViewModel
            )
        {
#if DEBUG
        // This suppresses annoying, useless output from WPF, as explained here:
        http://weblogs.asp.net/akjoshi/resolving-un-harmful-binding-errors-in-wpf
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Critical;
#endif
            messenger.RegisterAll(this);
            mainWindowViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(MainWindowViewModel.Title) &&
                    s is MainWindowViewModel vm)
                {
                    Text = vm.Title;
                }
            };
            
            InitializeComponent();

            AddMainPanelControl(mainPanelControl);

            this.KeyPreview = true;
            this.KeyDown += HandleKeyDown;
            
            TypeManager.Self.Initialize();

            // This has to happen before plugins are loaded since they may depend on settings...
            ProjectManager.Self.LoadSettings();
            
            ElementTreeViewManager.Self.Initialize();

            // ProperGridManager before MenuStripManager. Why does it need to be initialized before MainMenuStripPlugin?
            // Is htere a way to move this to a plugin?
            PropertyGridManager.Self.InitializeEarly();

            // bah we have to do this before initializing all plugins because we need the menu strip to exist:
            this.Controls.Add(MainMenuStrip = menuStripManager.CreateMenuStrip());

            PluginManager.Self.Initialize();

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

            periodicUiTimer.Tick += HandleFileWatchTimer;
            periodicUiTimer.Start(TimeSpan.FromSeconds(2));
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

        private void HandleFileWatchTimer()
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
