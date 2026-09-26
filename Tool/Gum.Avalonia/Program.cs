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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToolsUtilities;


namespace Gum.Avalonia;

/// <summary>Desktop entry point for the Avalonia head.</summary>
public static class Program
{
    private const string CrashLogsFolderName = "CrashLogs";

    /// <summary>Builds the service host, then runs the Avalonia application on this thread.</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        StartupTiming.Mark("Main entry");
        HeadOptions options = HeadOptions.Parse(args);
        // Before anything reads or writes a per-user file.
        FileManager.UserApplicationDataFolderOverride = options.UserDataFolder;

        // Set once Avalonia is set up; until then an error is logged but not shown.
        IServiceProvider? services = null;
        CrashReporter crashReporter = new CrashReporter(
            Path.Combine(GetAppDataDirectory(), CrashLogsFolderName),
            ToolVersion.Describe(typeof(Program).Assembly),
            message => ShowErrorMessage(services, options, message));
        UnhandledExceptionHooks.InstallProcessHooks(crashReporter);

        using IHost host = CreateHostBuilder(args)
            .ConfigureServices(serviceCollection => serviceCollection.AddSingleton<ICrashReporter>(crashReporter))
            .Build();
        StartupTiming.Mark("Host built");
        Locator.Register(host.Services);
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
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// The per-user Gum settings folder (honors <see cref="FileManager.UserApplicationDataFolderOverride"/>,
    /// e.g. the <c>--user-data</c> option, so tests and unattended runs don't touch the real one).
    /// Shared with <see cref="App"/> so freeze diagnostics land next to the rest of a user's Gum data.
    /// </summary>
    internal static string GetAppDataDirectory() =>
        FileManager.UserApplicationDataFolderOverride
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Gum");

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
                if (!File.Exists(settingsPath))
                {
                    File.WriteAllText(settingsPath, "{}");
                }

                cfg.Sources.Clear();
                cfg.SetBasePath(appDir);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
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
