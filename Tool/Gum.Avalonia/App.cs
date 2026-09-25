using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Diagnostics;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Shell.MacOS;
using Gum.Avalonia.Themes;
using Gum.CommandLine;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Diagnostics;
using Gum.Managers;
using Gum.Menus;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Startup;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia;

/// <summary>
/// The Avalonia application: shows the main window, then runs the shared
/// <see cref="GumStartupSequence"/> with this head's <see cref="AvaloniaHeadStartup"/> steps.
/// </summary>
public sealed class App : Application
{
    private readonly IServiceProvider _services;
    private readonly HeadOptions _options;
    private readonly IFreezeDiagnosticsInbox _freezeDiagnostics;
    private bool _previousSessionEndedDirty;

    /// <summary>Creates the app over the built service host.</summary>
    public App(IServiceProvider services, HeadOptions options)
    {
        _services = services;
        _options = options;
        _freezeDiagnostics = new FreezeDiagnosticsInbox(Path.Combine(Program.GetAppDataDirectory(), "FreezeDiagnostics"));
        // macOS names the app menu (its title, "Hide ...") from this, not from the bundle.
        Name = "Gum";
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        Name = "Gum";
        if (OperatingSystem.IsMacOS())
        {
            // The app menu must exist before Avalonia's post-setup exporter reads it, or it
            // supplies its own "About Avalonia" menu instead. The About action resolves on click
            // so no tool service is built this early.
            NativeMenu.SetMenu(this, AvaloniaNativeMenuBuilder.BuildAppMenu(
                () => _services.GetRequiredService<StandardMenuModelBuilder>().ShowAbout(),
                MenuTrackingScheduler.InvokeAfterTracking));
        }
        // Compact density: the WPF head's fields and rows are tighter than Fluent's defaults.
        Styles.Add(new FluentTheme { DensityStyle = DensityStyle.Compact });
        FrbThemeResources.Install(Resources);
        // ColorPicker ships its templates separately from the Fluent theme.
        Styles.Add(new StyleInclude(new Uri("avares://Gum/"))
        {
            Source = new Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml"),
        });
        Styles.Add(GumChromeStyles.Create());
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IMessenger messenger = _services.GetRequiredService<IMessenger>();
            IDisposable canvasInputHook = CanvasInputRedrawHook.Install(_services.GetRequiredService<ICanvasRedrawScheduler>());
            MainWindow window = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = window;
            StartupTiming.Mark("MainWindow resolved");
            _previousSessionEndedDirty = _freezeDiagnostics.BeginSession();

            desktop.Exit += (_, _) =>
            {
                List<Action> teardownActions = new List<Action>();
                messenger.Send(new ApplicationTeardownMessage(teardownActions));
                foreach (Action action in teardownActions)
                {
                    action();
                }
                canvasInputHook.Dispose();
                _freezeDiagnostics.EndSessionCleanly();
            };

            window.Opened += (_, _) =>
            {
                messenger.Send<ApplicationStartupMessage>();
                Dispatcher.UIThread.Post(() => _ = RunStartupAsync(desktop, window), DispatcherPriority.Background);
                if (_options.ExitAfterSeconds is double seconds)
                {
                    DispatcherTimer.RunOnce(() => CaptureAndExit(window, desktop), TimeSpan.FromSeconds(seconds));
                }
                StartFreezeWatchdog();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task RunStartupAsync(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        try
        {
            await new GumStartupSequence(_services, _services.GetRequiredService<AvaloniaHeadStartup>()).RunAsync();
            StartupTiming.Mark("InitializeGum complete");
            ApplyStartupSelection();
            ApplyThemeOverride();
            if (_services.GetRequiredService<ICommandLineManager>().ShouldExitImmediately)
            {
                desktop.Shutdown();
                return;
            }
        }
        catch (Exception exception)
        {
            // Also on stderr, so an unattended run (and HeadProcessTests) can see it.
            Console.Error.WriteLine("Startup failed: " + exception);
            window.ShowStartupFailure(exception);
            return;
        }

        // Nobody is there to answer in an unattended run, and the modal would hold up its exit timer.
        if (_options.ExitAfterSeconds == null)
        {
            PromptForUnreportedFreezeDiagnostics();
        }
    }

    // An unattended run can start with an element, and optionally one of its instances, selected.
    private void ApplyStartupSelection()
    {
        if (_options.SelectPath is not { } path)
        {
            return;
        }

        string[] parts = path.Split('#', 2);
        if (ObjectFinder.Self.GetElementSave(parts[0]) is not { } element)
        {
            return;
        }

        ISelectedState selectedState = _services.GetRequiredService<ISelectedState>();
        selectedState.SelectedElement = element;
        if (parts.Length > 1 && element.Instances.Find(instance => instance.Name == parts[1]) is { } selectedInstance)
        {
            selectedState.SelectedInstance = selectedInstance;
        }
    }

    // An unattended run can show the other theme variant without changing the saved setting.
    private void ApplyThemeOverride()
    {
        if (_options.Theme is not { } theme)
        {
            return;
        }

        RequestedThemeVariant = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        _services.GetRequiredService<IMessenger>().Send(new ThemeChangedMessage(_services.GetRequiredService<IThemingService>().EffectiveSettings));
    }

    // See issue #4781: a permanent, debugger-independent freeze reported between giving a rename/add-state
    // command and its popup appearing. The heartbeat timer proves the UI thread is still pumping; the
    // watchdog itself decides (off the timer) whether a missed heartbeat means it has stalled.
    private void StartFreezeWatchdog()
    {
        UiFreezeWatchdog.Start(_freezeDiagnostics.DirectoryPath);
        DispatcherTimer heartbeat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        heartbeat.Tick += (_, _) => UiFreezeWatchdog.Heartbeat();
        heartbeat.Start();
    }

    // See issue #4848: a frozen Gum gets killed, so the only moment to tell the user the watchdog
    // captured something is the next launch, once the main window can own the dialog.
    private void PromptForUnreportedFreezeDiagnostics()
    {
        new FreezeDiagnosticsPromptService(
                _freezeDiagnostics,
                _services.GetRequiredService<IDialogService>(),
                _services.GetRequiredService<IFileSystemRevealService>())
            .PromptIfNeeded(_previousSessionEndedDirty);
    }

    private void CaptureAndExit(Window window, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_options.ScreenshotPath != null)
        {
            PixelSize pixelSize = new PixelSize(
                (int)Math.Round(window.Bounds.Width * window.RenderScaling),
                (int)Math.Round(window.Bounds.Height * window.RenderScaling));
            using RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * window.RenderScaling, 96 * window.RenderScaling));
            bitmap.Render(window);
            Directory.CreateDirectory(Path.GetDirectoryName(_options.ScreenshotPath)!);
            bitmap.Save(_options.ScreenshotPath);
        }

        desktop.Shutdown();
    }
}
