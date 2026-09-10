using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.CommandLine;
using Gum.Diagnostics;
using Gum.Startup;
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

    /// <summary>Creates the app over the built service host.</summary>
    public App(IServiceProvider services, HeadOptions options)
    {
        _services = services;
        _options = options;
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IMessenger messenger = _services.GetRequiredService<IMessenger>();
            MainWindow window = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = window;
            StartupTiming.Mark("MainWindow resolved");

            desktop.Exit += (_, _) =>
            {
                List<Action> teardownActions = new List<Action>();
                messenger.Send(new ApplicationTeardownMessage(teardownActions));
                foreach (Action action in teardownActions)
                {
                    action();
                }
            };

            window.Opened += (_, _) =>
            {
                messenger.Send<ApplicationStartupMessage>();
                Dispatcher.UIThread.Post(() => _ = RunStartupAsync(desktop, window), DispatcherPriority.Background);
                if (_options.ExitAfterSeconds is double seconds)
                {
                    DispatcherTimer.RunOnce(() => CaptureAndExit(window, desktop), TimeSpan.FromSeconds(seconds));
                }
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
            if (_services.GetRequiredService<ICommandLineManager>().ShouldExitImmediately)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception exception)
        {
            window.ShowStartupFailure(exception);
        }
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
