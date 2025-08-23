using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Plugins;
using Gum.Reflection;
using Gum.Wireframe;
using Gum.PropertyGridHelpers;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Controls;
using Gum.ViewModels;
using SystemColors = System.Windows.SystemColors;

namespace Gum;
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

public partial class MainWindow : Window, IRecipient<CloseMainWindowMessage>
{
    #region Fields/Properties

    private readonly IGuiCommands _guiCommands;

    #endregion
    
    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        MenuStripManager menuStripManager,
        IGuiCommands guiCommands,
        IMessenger messenger,
        HotkeyManager hotkeyManager
        )
    {
        DataContext = mainWindowViewModel;
        _guiCommands = guiCommands;
        
        messenger.RegisterAll(this);
        
        InitializeComponent();
        this.WinformsMenuHost.Child = menuStripManager.CreateMenuStrip();

        this.PreviewKeyDown += (_,e) => hotkeyManager.PreviewKeyDownAppWide(e);
        this.Loaded += (_, _) => mainWindowViewModel.LoadWindowSettings();
        this.Closed += (_, _) => mainWindowViewModel.SaveWindowSettings();
        this.Background = SystemColors.ControlBrush;
    }

    void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message)
    {
        Close();
    }
}

public record CloseMainWindowMessage;
