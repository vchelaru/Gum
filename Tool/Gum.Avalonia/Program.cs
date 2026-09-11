using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Gum.Avalonia.Services;
using Gum.Diagnostics;
using Gum.Services;
using Gum.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gum.Avalonia;

/// <summary>Desktop entry point for the Avalonia head.</summary>
public static class Program
{
    /// <summary>Builds the service host, then runs the Avalonia application on this thread.</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        StartupTiming.Mark("Main entry");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Console.Error.WriteLine("Unhandled exception: " + e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            Console.Error.WriteLine("Unobserved task exception: " + e.Exception);

        HeadOptions options = HeadOptions.Parse(args);
        using IHost host = CreateHostBuilder(args).Build();
        StartupTiming.Mark("Host built");
        Locator.Register(host.Services);
        host.StartAsync().GetAwaiter().GetResult();

        int exitCode = BuildAvaloniaApp(host.Services, options).StartWithClassicDesktopLifetime(args);

        host.StopAsync().GetAwaiter().GetResult();
        return exitCode;
    }

    /// <summary>The Avalonia app builder; also used by the headless tests.</summary>
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services, HeadOptions options) =>
        AppBuilder.Configure(() => new App(services, options))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// The same host shape as the WPF head: settings from the per-user Gum folder, the headless
    /// core, and this head's implementations of the head-provided contracts.
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Gum");
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
