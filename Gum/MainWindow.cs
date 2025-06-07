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
using Gum.Plugins.InternalPlugins.MenuStripPlugin;
using GumRuntime;

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

        MainPanelControl mainPanelControl;

        #endregion

        public MainWindow(MainWindowViewModel mainWindowViewModel)
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
            
            mainWindowViewModel.Initialize(this, mainPanelControl, components, ElementTreeImages);
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
