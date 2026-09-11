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
using Gum.Avalonia.Themes;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Gum.Services;
using Gum.Settings;
using Gum.ViewModels;
using AvaloniaBinding = Avalonia.Data.Binding;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Gum.Dialogs;

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
    private global::Avalonia.Controls.Image _logo = null!;

    // The WPF head's caption height (its caption buttons are 48 by 32).
    private const double TitleBarHeight = 32;

    private static readonly IValueConverter FileNameOnly =
        new FuncValueConverter<string?, string?>(title => string.IsNullOrEmpty(title) ? title : System.IO.Path.GetFileNameWithoutExtension(title));

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
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Gum.Avalonia/GumIcon.ico")));
        Width = WindowSettings.DefaultWidth;
        Height = WindowSettings.DefaultHeight;
        MinWidth = 640;
        MinHeight = 400;
        FontSize = appScaleProvider.BaseFontSize;
        this.WithThemeResource(BackgroundProperty, "Frb.Brushes.Background");
        this.WithThemeResource(ForegroundProperty, "Frb.Brushes.Foreground");
        if (appScaleProvider is AvaloniaAppScaleProvider scale)
        {
            scale.BaseFontSizeChanged += () => FontSize = scale.BaseFontSize;
        }
        this.Bind(TitleProperty, new AvaloniaBinding(nameof(ShellViewModel.Title)));

        Menu menu = AvaloniaMenuBuilder.Build(menuBuilder.Build());
        Control titleRow = CreateTitleRow(menu);
        DockPanel.SetDock(titleRow, Dock.Top);

        _statusText = new TextBlock { Margin = new Thickness(8, 2), VerticalAlignment = VerticalAlignment.Center };
        _statusText.Bind(TextBlock.TextProperty, new AvaloniaBinding(nameof(ShellViewModel.ProgressText)));
        Border statusBar = new Border
        {
            Child = _statusText,
            Height = 24,
            BorderThickness = new Thickness(0, 1, 0, 0),
        }.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Border");
        DockPanel.SetDock(statusBar, Dock.Bottom);

        DockPanel root = new DockPanel();
        root.Children.Add(titleRow);
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
            else if (e.Property == OffScreenMarginProperty)
            {
                // A maximized window's frame hangs past the screen edges; with the content drawn into
                // the title bar, keep it inside the visible area.
                root.Margin = OffScreenMargin;
            }
        };

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => _modifierKeyState.Current = e.KeyModifiers, RoutingStrategies.Tunnel);
    }

    /// <summary>Shows a startup failure in place of the panels, so an unattended run captures it.</summary>
    // As the WPF head's window chrome: the Gum logo, the menu, and the project file's name share the
    // title bar, beside the system's caption buttons. Where the platform cannot draw into the title
    // bar (Linux), the system title bar stays and this row sits under it.
    private Control CreateTitleRow(Menu menu)
    {
        bool drawsInTitleBar = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        if (drawsInTitleBar)
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaTitleBarHeightHint = TitleBarHeight;
        }

        _logo = new global::Avalonia.Controls.Image
        {
            Height = 16,
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = global::Avalonia.Media.Stretch.Uniform,
        };
        RefreshLogo();
        ActualThemeVariantChanged += (_, _) => RefreshLogo();

        menu.VerticalAlignment = VerticalAlignment.Center;
        menu.Background = global::Avalonia.Media.Brushes.Transparent;
        Grid.SetColumn(menu, 1);

        TextBlock fileName = new TextBlock
        {
            Margin = new Thickness(6, 4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis,
        };
        fileName.Bind(TextBlock.TextProperty, new AvaloniaBinding(nameof(ShellViewModel.Title)) { Converter = FileNameOnly });
        fileName.Bind(ToolTip.TipProperty, new AvaloniaBinding(nameof(ShellViewModel.Title)));
        Grid.SetColumn(fileName, 2);

        Grid row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            // Room for the caption buttons: on the right on Windows, the traffic lights on the left on macOS.
            Margin = !drawsInTitleBar ? new Thickness(0)
                : OperatingSystem.IsMacOS() ? new Thickness(72, 0, 0, 0)
                : new Thickness(0, 0, 140, 0),
        };
        if (drawsInTitleBar)
        {
            row.Height = TitleBarHeight;
        }
        row.Children.Add(_logo);
        row.Children.Add(menu);
        row.Children.Add(fileName);
        return row;
    }

    // The shared light/dark logo choice (MainWindowIconLogic), loaded from this head's resources.
    private void RefreshLogo()
    {
        ThemeMode mode = ActualThemeVariant == ThemeVariant.Light ? ThemeMode.Light : ThemeMode.Dark;
        string logoFile = MainWindowIconLogic.GetIconSource(mode).Split('/')[^1];
        _logo.Source = new global::Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new Uri("avares://Gum.Avalonia/" + logoFile)));
    }

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
