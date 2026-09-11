using System;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Services;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Gum.Services;
using Gum.Settings;
using Gum.ViewModels;
using AvaloniaBinding = Avalonia.Data.Binding;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The tool's main window: menu on top, the five-region panel in the middle, a status bar at the
/// bottom. Routes window-wide key presses into the hotkey manager and restores/persists placement.
/// </summary>
public sealed class MainWindow : Window, IRecipient<CloseMainWindowMessage>
{
    private readonly ShellViewModel _shell;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly AvaloniaModifierKeyState _modifierKeyState;
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;
    private readonly TextBlock _statusText;

    /// <summary>Builds the window; nothing here touches the project until the startup sequence runs.</summary>
    public MainWindow(
        ShellViewModel shell,
        AvaloniaTabManager tabs,
        StandardMenuModelBuilder menuBuilder,
        IHotkeyManager hotkeyManager,
        AvaloniaModifierKeyState modifierKeyState,
        IMessenger messenger,
        IWritableOptions<LayoutSettings> layoutSettings,
        IAppScaleProvider appScaleProvider)
    {
        _shell = shell;
        _hotkeyManager = hotkeyManager;
        _modifierKeyState = modifierKeyState;
        _layoutSettings = layoutSettings;
        DataContext = shell;
        messenger.RegisterAll(this);

        Title = shell.Title;
        Width = WindowSettings.DefaultWidth;
        Height = WindowSettings.DefaultHeight;
        MinWidth = 640;
        MinHeight = 400;
        FontSize = appScaleProvider.BaseFontSize;
        if (appScaleProvider is AvaloniaAppScaleProvider scale)
        {
            scale.BaseFontSizeChanged += () => FontSize = scale.BaseFontSize;
        }
        this.Bind(TitleProperty, new AvaloniaBinding(nameof(ShellViewModel.Title)));

        Menu menu = AvaloniaMenuBuilder.Build(menuBuilder.Build());
        DockPanel.SetDock(menu, Dock.Top);

        _statusText = new TextBlock { Margin = new Thickness(8, 2), VerticalAlignment = VerticalAlignment.Center };
        _statusText.Bind(TextBlock.TextProperty, new AvaloniaBinding(nameof(ShellViewModel.ProgressText)));
        Border statusBar = new Border
        {
            Child = _statusText,
            Height = 24,
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromArgb(255, 60, 60, 60)),
        };
        DockPanel.SetDock(statusBar, Dock.Bottom);

        DockPanel root = new DockPanel();
        root.Children.Add(menu);
        root.Children.Add(statusBar);
        root.Children.Add(new MainPanelView(tabs));
        Content = root;

        Opened += (_, _) => RestorePlacement();
        PositionChanged += (_, _) => { if (WindowState == WindowState.Normal) { _shell.Left = Position.X; _shell.Top = Position.Y; } };
        SizeChanged += (_, _) => { if (WindowState == WindowState.Normal) { _shell.Width = Bounds.Width; _shell.Height = Bounds.Height; } };
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
            {
                _shell.WindowState = WindowState == WindowState.Maximized ? GumWindowState.Maximized : GumWindowState.Normal;
            }
        };

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => _modifierKeyState.Current = e.KeyModifiers, RoutingStrategies.Tunnel);
    }

    /// <summary>Shows a startup failure in place of the panels, so an unattended run captures it.</summary>
    public void ShowStartupFailure(Exception exception)
    {
        Content = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = "Startup failed:\n\n" + exception,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(16),
            },
        };
    }

    void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message) => Close();

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        _modifierKeyState.Current = e.KeyModifiers;
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        // Ctrl+= / Ctrl+- zoom the whole app unless a canvas that owns them has focus (phase 50).
        // A render canvas that owns Ctrl+=/Ctrl+- for its own camera opts out of the app-wide zoom.
        _hotkeyManager.PreviewKeyDownAppWide(keyArgs, enableEntireAppZoom: CameraZoomScope.IsEntireAppZoomEnabledFor(e.Source));
        e.Handled = keyArgs.Handled;
    }

    private void RestorePlacement()
    {
        WindowSettings saved = _layoutSettings.CurrentValue.MainWindow;
        PixelPoint probe = saved.Left is double left && saved.Top is double top ? new PixelPoint((int)left, (int)top) : Position;
        Screen? screen = Screens.ScreenFromPoint(probe) ?? Screens.Primary;
        Rectangle workingArea = screen == null
            ? new Rectangle(0, 0, (int)WindowSettings.DefaultWidth, (int)WindowSettings.DefaultHeight)
            : new Rectangle(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);

        _shell.LoadWindowSettings(saved, workingArea);
        if (_shell.IsFirstLaunch)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        Position = new PixelPoint((int)_shell.Left, (int)_shell.Top);
        Width = _shell.Width;
        Height = _shell.Height;
        WindowState = _shell.WindowState == GumWindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
    }
}
