using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Gum.Diagnostics;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>The environment for the Avalonia head: adds the main window's screen scaling to the display scale.</summary>
public sealed class AvaloniaDiagnosticsEnvironment : DiagnosticsEnvironment
{
    /// <summary>Creates the environment.</summary>
    public AvaloniaDiagnosticsEnvironment(IAppScaleProvider appScaleProvider) : base(appScaleProvider)
    {
    }

    /// <inheritdoc/>
    protected override string GetDisplayScale()
    {
        string zoom = base.GetDisplayScale();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return $"screen {window.RenderScaling:P0}, {zoom}";
        }
        return zoom;
    }
}
