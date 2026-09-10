using Avalonia;

namespace AvaloniaCanvasSpike;

/// <summary>Desktop entry point.</summary>
public static class Program
{
    /// <summary>The backend name is set by each head's csproj through a compile constant.</summary>
#if KNI_BACKEND
    private const string BackendName = "KNI SDL2.GL";
#else
    private const string BackendName = "MonoGame DesktopGL";
#endif

    /// <summary>Starts Avalonia with the classic desktop lifetime.</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        SpikeOptions options = SpikeOptions.Parse(BackendName, args);
        return AppBuilder.Configure(() => new App(options))
            .UsePlatformDetect()
            .WithInterFont()
            .StartWithClassicDesktopLifetime(args);
    }
}
