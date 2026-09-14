using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AvaloniaCanvasSpike;

/// <summary>Code-only Avalonia application: one window hosting the canvas.</summary>
public sealed class App : Application
{
    private readonly SpikeOptions _options;

    /// <summary>Creates the application with the parsed command line.</summary>
    public App(SpikeOptions options)
    {
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
            Window window = new Window
            {
                Title = $"Gum canvas spike ({_options.BackendName}) - {_options.ProjectPath}",
                Width = 1024,
                Height = 720,
                Content = new CanvasView(() => new GumCanvasGame(_options.ProjectPath, _options.ElementName)),
            };
            desktop.MainWindow = window;

            if (_options.ExitAfterSeconds is double seconds)
            {
                // Unattended runs: capture the window and quit, so a build agent can verify output.
                window.Opened += (_, _) => DispatcherTimer.RunOnce(
                    () => CaptureAndExit(window, desktop),
                    TimeSpan.FromSeconds(seconds));
            }
        }

        base.OnFrameworkInitializationCompleted();
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
