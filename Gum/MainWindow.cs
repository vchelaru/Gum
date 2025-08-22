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

        #endregion

        public MainWindow(MainPanelControl mainPanelControl,
            MainWindowViewModel mainWindowViewModel,
            MenuStripManager menuStripManager,
            IGuiCommands guiCommands,
            IMessenger messenger
            )
        {
            _guiCommands = guiCommands;
            
            messenger.RegisterAll(this);
            mainWindowViewModel.PropertyChanged += (s, e) =>
            {
                if (s is not MainWindowViewModel vm)
                {
                    return;
                }

                switch (e.PropertyName)
                {
                    case nameof(MainWindowViewModel.Title):
                        Text = vm.Title;
                        break;
                    case nameof(MainWindowViewModel.Bounds) when vm.Bounds is { } bounds:
                        Bounds = bounds;
                        break;
                    case nameof(MainWindowViewModel.WindowState) when vm.WindowState is { } windowState:
                        WindowState = windowState;
                        break;
                }
            };
            
            InitializeComponent();
            
            this.Controls.Add(menuStripManager.CreateMenuStrip());
            AddMainPanelControl(mainPanelControl);

            this.KeyPreview = true;
            this.KeyDown += HandleKeyDown;
            this.Load += (_, _) => mainWindowViewModel.LoadWindowSettings();
            this.FormClosed += (_, _) => mainWindowViewModel.SaveWindowSettings(Bounds, WindowState);
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
            if (args.KeyCode == Keys.F && 
                (args.Modifiers & Keys.Control) == Keys.Control)
            {
                _guiCommands.FocusSearch();
                args.Handled = true;
                args.SuppressKeyPress = true;
            }
        }

        void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message)
        {
            Close();
        }
    }
    
    public record CloseMainWindowMessage;
}
