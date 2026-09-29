using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
using Gum.Plugins.InternalPlugins.EditorTab.Services;
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
        // macOS sends a Finder-opened document as an activation, not a command-line argument.
        // Subscribe before the main loop starts so one sent at launch isn't missed (#5130).
        if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            FileActivationHandler fileActivationHandler = new FileActivationHandler(
                _services.GetRequiredService<Lazy<IProjectOpenRequestRouter>>());
            activatableLifetime.Activated += fileActivationHandler.HandleActivated;
        }
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
        if (OperatingSystem.IsMacOS())
        {
            Styles.Add(GumChromeStyles.CreateMacOS());
        }
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IMessenger messenger = _services.GetRequiredService<IMessenger>();
            IDisposable canvasInputHook = CanvasInputRedrawHook.Install(_services.GetRequiredService<ICanvasRedrawScheduler>());
            MainWindow window = _services.GetRequiredService<MainWindow>();
            // Agents and CI run this hundreds of times; don't take the user's focus.
            if (_options.ExitAfterSeconds != null)
            {
                window.ShowInBackground();
            }
            desktop.MainWindow = window;
            StartupTiming.Mark("MainWindow resolved");
            _previousSessionEndedDirty = _freezeDiagnostics.BeginSession();

            desktop.Exit += (_, _) =>
            {
                new ApplicationTeardown(messenger).Run();
                canvasInputHook.Dispose();
                _freezeDiagnostics.EndSessionCleanly();
            };

            window.Opened += (_, _) =>
            {
                messenger.Send<ApplicationStartupMessage>();
                TaskCompletionSource<UnattendedStartupOutcome> startup = new TaskCompletionSource<UnattendedStartupOutcome>();
                StartupFailureReporter failureReporter = new StartupFailureReporter(window, Console.Error);
                Dispatcher.UIThread.Post(() => _ = SignalStartupAsync(startup, desktop, failureReporter), DispatcherPriority.Background);
                if (_options.ExitAfterSeconds is double seconds)
                {
                    _ = RunUnattendedAsync(startup.Task, seconds, window, desktop, messenger, failureReporter);
                }
                StartFreezeWatchdog();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task SignalStartupAsync(TaskCompletionSource<UnattendedStartupOutcome> startup,
        IClassicDesktopStyleApplicationLifetime desktop, StartupFailureReporter failureReporter) =>
        startup.SetResult(await RunStartupAsync(desktop, failureReporter));

    private async Task<UnattendedStartupOutcome> RunStartupAsync(IClassicDesktopStyleApplicationLifetime desktop,
        StartupFailureReporter failureReporter)
    {
        UnattendedStartupOutcome outcome = await new HeadStartupRun(failureReporter).RunAsync(() => RunStartupStepsAsync(desktop));

        // Nobody is there to answer in an unattended run, and the modal would hold up its exit.
        if (outcome == UnattendedStartupOutcome.Ready && _options.ExitAfterSeconds == null)
        {
            PromptForUnreportedFreezeDiagnostics();
            _services.GetService<ICrashReporter>()?.PromptForPreviousCrash();
        }
        return outcome;
    }

    private async Task<UnattendedStartupOutcome> RunStartupStepsAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        await new GumStartupSequence(_services, _services.GetRequiredService<AvaloniaHeadStartup>()).RunAsync();
        StartupTiming.Mark("InitializeGum complete");
        ApplyStartupSelection();
        ApplyThemeOverride();
        ICommandLineManager commandLine = _services.GetRequiredService<ICommandLineManager>();
        if (!commandLine.ShouldExitImmediately)
        {
            return UnattendedStartupOutcome.Ready;
        }

        // A script running a command-line option sees why it didn't run, and a failure code.
        if (commandLine.UsageError is { } usageError)
        {
            Console.Error.WriteLine(usageError);
            desktop.Shutdown(1);
        }
        else
        {
            desktop.Shutdown();
        }
        return UnattendedStartupOutcome.ExitRequested;
    }

    // --exit-after is the upper bound (#5170): the run captures and exits once startup has finished
    // and the canvas has drawn, and exits nonzero if that hasn't happened in time.
    private async Task RunUnattendedAsync(Task<UnattendedStartupOutcome> startup, double exitAfterSeconds,
        Window window, IClassicDesktopStyleApplicationLifetime desktop, IMessenger messenger,
        StartupFailureReporter failureReporter)
    {
        int? exitCode;
        try
        {
            exitCode = await UnattendedRun.RunAsync(
                startup,
                Task.Delay(TimeSpan.FromSeconds(exitAfterSeconds)),
                exitAfterSeconds,
                nextCanvasFrame: async () => await messenger.Send(new EditorCanvasFrameRequestMessage()),
                zoomToFit: _options.ZoomToFit ? () => messenger.Send(new ZoomCanvasToFitSelectionMessage()).Response?.ToString() : null,
                capture: _options.ScreenshotPath is { } path ? () => CaptureWindow(window, path) : null,
                Console.Out,
                Console.Error);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("The unattended run failed: " + exception);
            exitCode = 1;
        }

        if (exitCode is int code)
        {
            // EXPERIMENT (#5476): open and close windows so Avalonia's DBus menu exporter is disposed.
            for (int i = 0; i < 3; i++)
            {
                Window probe = new Window { Width = 200, Height = 100, Title = "probe " + i };
                // Closed in the same UI job, before the RegisterWindow reply's continuation runs.
                probe.Show();
                probe.Close();
                await Task.Delay(1000);
                Console.WriteLine("EXPERIMENT: closed probe window " + i);
            }

            // A faulted task nobody awaited is reported only when the GC finalizes it, which a short
            // run may never do; collect now so its crash log is written before the run ends.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            failureReporter.OnUnattendedExitStarted();
            desktop.Shutdown(code);
        }
    }


    // An unattended run can start with an element, and optionally one of its instances, selected.
    private void ApplyStartupSelection()
    {
        if (_options.SelectPath is { } path)
        {
            SelectStartupPath(path, _services.GetRequiredService<ISelectedState>());
        }
    }

    /// <summary>
    /// Selects the element named before the <c>#</c> of <paramref name="path"/> and, when one
    /// follows it, that element's instance; an element that doesn't exist selects nothing.
    /// </summary>
    internal static void SelectStartupPath(string path, ISelectedState selectedState)
    {
        string[] parts = path.Split('#', 2);
        if (ObjectFinder.Self.GetElementSave(parts[0]) is not { } element)
        {
            return;
        }

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

        RequestedThemeVariant = ThemeVariantFor(theme);
        _services.GetRequiredService<IMessenger>().Send(new ThemeChangedMessage(_services.GetRequiredService<IThemingService>().EffectiveSettings));
    }

    /// <summary>The variant <c>--theme</c> names: "light" in any case is light, anything else dark.</summary>
    internal static ThemeVariant ThemeVariantFor(string theme) =>
        string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ? ThemeVariant.Light : ThemeVariant.Dark;

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

    private static void CaptureWindow(Window window, string path)
    {
        PixelSize pixelSize = new PixelSize(
            (int)Math.Round(window.Bounds.Width * window.RenderScaling),
            (int)Math.Round(window.Bounds.Height * window.RenderScaling));
        using RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * window.RenderScaling, 96 * window.RenderScaling));
        bitmap.Render(window);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        bitmap.Save(path);
    }
}
