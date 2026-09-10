using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

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
            desktop.MainWindow = new Window
            {
                Title = $"Gum canvas spike ({_options.BackendName}) - {_options.ProjectPath}",
                Width = 1024,
                Height = 720,
                Content = new CanvasView(() => new GumCanvasGame(_options.ProjectPath, _options.ElementName)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
