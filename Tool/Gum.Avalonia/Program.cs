using System;
using System.IO;
using Avalonia;
using Avalonia.Threading;
using Gum.Avalonia.Diagnostics;
using Gum.Avalonia.Services;
using Gum.Diagnostics;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Gum.Startup;
using Gum.Startup.SingleInstance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToolsUtilities;


namespace Gum.Avalonia;

/// <summary>Desktop entry point for the Avalonia head.</summary>
public static class Program
{
    private const string CrashLogsFolderName = "CrashLogs";

    // Held so the thread-pool timer isn't collected while the run is alive.
    private static UnattendedExitDeadline? _exitDeadline;

    /// <summary>Builds the service host, then runs the Avalonia application on this thread.</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        StartupTiming.Mark("Main entry");
        HeadOptions options = HeadOptions.Parse(args);
        if (options.ExitAfterSeconds != null && options.StartupFileError is { } startupFileError)
        {
            Console.Error.WriteLine(startupFileError + ". No screenshot was taken.");
            return 1;
        }

        // Before anything reads or writes a per-user file.
        ApplyUserDataFolder(options);
        StartupJitProfile.Start(GetAppDataDirectory());

        // Before a window or a project load: a launch that finds the project already owned hands
        // its request over and exits, and the owner's pipe exists before the slow startup begins.
        using SingleInstanceGate? gate = EnterSingleInstanceGate(options);
        if (gate?.Outcome == GateOutcome.HandedOff)
        {
            Console.Out.WriteLine("Handed " + options.HandoffRequest!.Project + " to the running Gum.");
            return 0;
        }

        if (options.ExitAfterSeconds is double exitAfterSeconds)
        {
            _exitDeadline = UnattendedExitDeadline.Start(UnattendedExitDeadline.For(exitAfterSeconds), ForceExit);
        }

        // Set once Avalonia is set up; until then an error is logged but not shown.
        IServiceProvider? services = null;
        CrashReporter crashReporter = new CrashReporter(
            Path.Combine(GetAppDataDirectory(), CrashLogsFolderName),
            ToolVersion.Describe(typeof(Program).Assembly),
            message => ShowErrorMessage(services, options, message));
        UnhandledExceptionHooks.InstallProcessHooks(crashReporter);

        using IHost host = CreateHostBuilder(args)
            .ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddSingleton<ICrashReporter>(crashReporter);
                if (gate?.Inbox is { } inbox)
                {
                    serviceCollection.AddSingleton<IOpenRequestInbox>(inbox);
                }
            })
            .Build();
        StartupTiming.Mark("Host built");
        Locator.Register(host.Services);
        if (gate != null)
        {
            // The claim follows the project this Gum has open, so a launch for any other project
            // opens its own Gum and a launch for this one reaches this Gum.
            host.Services.GetRequiredService<IProjectOpenRequestRouter>().ProjectPathChanged += path => gate.Reclaim(path);
        }

        host.StartAsync().GetAwaiter().GetResult();

        int exitCode = BuildAvaloniaApp(host.Services, options)
            .AfterSetup(_ =>
            {
                UnhandledExceptionHooks.InstallDispatcherHook(crashReporter);
                services = host.Services;
            })
            .StartWithClassicDesktopLifetime(args);

        host.StopAsync().GetAwaiter().GetResult();
        return exitCode;
    }

    // Null when the launch does not take part. A launch that names a project waits for an owner that
    // is still starting, so it is delivered instead of opening a second copy. One that names none
    // claims the project it is about to reopen, so a launch during its startup reaches it.
    private static SingleInstanceGate? EnterSingleInstanceGate(HeadOptions options)
    {
        if (!options.TakesPartInHandoff)
        {
            return null;
        }

        // Gum always starts: a claim that fails for any reason leaves this Gum unclaimed.
        try
        {
            return EnterSingleInstanceGateCore(options);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("The single-instance claim failed (" + exception.Message + "). Starting a new Gum.");
            ToolLog.WriteException("single-instance gate", exception);
            return null;
        }
    }

    private static SingleInstanceGate EnterSingleInstanceGateCore(HeadOptions options)
    {
        string instanceDirectory = InstanceIdentity.DefaultInstanceDirectory();
        string userName = Environment.UserName;
        bool caseSensitive = InstanceIdentity.IsFileSystemCaseSensitive();
        if (options.HandoffRequest == null)
        {
            SingleInstanceGate unclaimed = SingleInstanceGate.StartUnclaimed(instanceDirectory, userName, caseSensitive);
            unclaimed.Reclaim(LastOpenedProject());
            return unclaimed;
        }

        SingleInstanceGate gate = SingleInstanceGate.Enter(
            options.HandoffRequest,
            instanceDirectory,
            userName,
            caseSensitive,
            TimeSpan.FromSeconds(15),
            WindowsForegroundPermission.Allow);
        if (gate.Outcome == GateOutcome.Unclaimed && gate.Message != null)
        {
            Console.Error.WriteLine(gate.Message + " Starting a new Gum.");
        }

        return gate;
    }

    // The project startup will reopen unless something stops it; a wrong guess is corrected when the
    // project that did open reports its path.
    private static string? LastOpenedProject() =>
        GeneralSettingsFile.LoadOrCreateNew().LastProject is { Length: > 0 } last && File.Exists(last) ? last : null;

    /// <summary>
    /// Points every per-user file (settings, recent projects, crash logs) at <c>--user-data</c>'s
    /// folder, or at the user's own folder when the option is absent.
    /// </summary>
    internal static void ApplyUserDataFolder(HeadOptions options) =>
        FileManager.UserApplicationDataFolderOverride = options.UserDataFolder;

    // Killed rather than Environment.Exit: the generic host's ProcessExit handler waits for the host
    // to be disposed, which never happens while Main is stuck.
    private static void ForceExit(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.Flush();
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    // Posted, so the dialog never opens inside the exception handler that reported the error. An
    // unattended run has nobody to answer it, and the modal would hold up its exit timer.
    private static void ShowErrorMessage(IServiceProvider? services, HeadOptions options, string message)
    {
        if (services == null || options.ExitAfterSeconds != null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => services.GetRequiredService<IDialogService>().ShowMessage(message, "Gum Error"));
    }

    /// <summary>The Avalonia app builder; also used by the headless tests.</summary>
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services, HeadOptions options) =>
        AppBuilder.Configure(() => new App(services, options))
            .UsePlatformDetect()
            // Gum sets a NativeMenu only on macOS, so exporting one over DBus on Linux does nothing
            // except call a registrar many desktops (Cinnamon, GNOME) don't have.
            .With(new X11PlatformOptions { UseDBusMenu = false })
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// The per-user Gum settings folder, <see cref="FileManager.UserApplicationDataForThisApplication"/>
    /// without its trailing separator. It honors the <c>--user-data</c> override, so tests and
    /// unattended runs don't touch the real one.
    /// Shared with <see cref="App"/> so freeze diagnostics land next to the rest of a user's Gum data.
    /// </summary>
    internal static string GetAppDataDirectory() =>
        FileManager.UserApplicationDataForThisApplication.TrimEnd(Path.DirectorySeparatorChar);

    /// <summary>
    /// The same host shape as the WPF head: settings from the per-user Gum folder, the headless
    /// core, and this head's implementations of the head-provided contracts.
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        string appDir = GetAppDataDirectory();
        Directory.CreateDirectory(appDir);
        string settingsPath = Path.Combine(appDir, "appsettings.json");

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
            {
                string? unreadableCopy = AppSettingsFile.EnsureLoadable(settingsPath);
                if (unreadableCopy != null)
                {
                    Console.Error.WriteLine($"Could not read {settingsPath}; started with default theme and layout. The old file was moved to {unreadableCopy}.");
                }

                cfg.Sources.Clear();
                cfg.SetBasePath(appDir);
                cfg.AddAppSettingsJsonFile("appsettings.json");
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions();
                services.ConfigureWritable<ThemeSettings>(context.Configuration, nameof(ThemeSettings), settingsPath);
                services.ConfigureWritable<LayoutSettings>(context.Configuration, nameof(LayoutSettings), settingsPath);
                services.AddGumCore();
                services.AddGumAvalonia();
            });
    }
}
