namespace Gum.Services;

/// <summary>
/// Reads and writes the tool's UI base font size. Abstracts the WPF-typed <c>AppScale</c> theme
/// resource so <see cref="UiSettingsService"/> can live in the headless assembly (ADR-0005). See
/// <see cref="Gum.Services.AppScaleProvider"/> for the concrete implementation (tool project).
/// </summary>
public interface IAppScaleProvider
{
    /// <summary>Gets or sets the base font size the tool's UI theme scales from.</summary>
    double BaseFontSize { get; set; }
}
