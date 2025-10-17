using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services;
using Gum.Settings;
using Gum.ViewModels;
using System;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using RoutedEventArgs = System.Windows.RoutedEventArgs;

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
        HotkeyManager hotkeyManager,
        IWritableOptions<LayoutSettings> layoutSettings
        )
    {
        DataContext = mainWindowViewModel;
        _guiCommands = guiCommands;
        
        messenger.RegisterAll(this);
        
        InitializeComponent();
        this.WinformsMenuHost.Child = menuStripManager.CreateMenuStrip();

        this.PreviewKeyDown += (_,e) => hotkeyManager.PreviewKeyDownAppWide(e);
        this.Loaded += (_, _) =>
        {
            mainWindowViewModel.LoadWindowSettings(layoutSettings.CurrentValue.MainWindow);
        };
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