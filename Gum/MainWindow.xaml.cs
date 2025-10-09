using System;
using System.Windows;
using Gum.Managers;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Services;
using Gum.ViewModels;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Gum.Settings;

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
        //this.Background = SystemColors.ControlBrush;
    }

    void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message)
    {
        Close();
    }

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeButtonClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Locator.GetRequiredService<IMessenger>().Send(new CloseMainWindowMessage());
    }
}

public record CloseMainWindowMessage;
public record ThemeChangedMessage(IEffectiveThemeSettings settings);