using System.Drawing;

namespace Gum.Dialogs;

public enum ThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>
/// Split out of <see cref="ThemingDialogViewModel"/>'s original combined file (ADR-0005) so it can
/// live in the headless Gum.Presentation assembly - both this interface and the colors it exposes
/// use only <see cref="System.Drawing.Color"/> (ADR-0004), no WPF types. The concrete
/// implementation (<c>Gum.Dialogs.ThemingService</c>) stays in the Gum tool project, since it drives
/// WPF resource dictionaries and brushes directly.
/// </summary>
public interface IThemingService
{
    IEffectiveThemeSettings EffectiveSettings { get; }
    ThemeMode? Mode { get; set; }
    Color? Accent { get; set; }
    Color? CheckerA { get; set; }
    Color? CheckerB { get; set; }
    Color? OutlineColor { get; set; }
    Color? GuideLine { get; set; }
    Color? GuideText { get; set; }
    bool IsSystemInDarkMode { get; }
    void ApplyInitialTheme();
}

/// <summary>
/// The resolved (non-null) theme colors <see cref="IThemingService"/> falls back to when the user
/// hasn't made an explicit choice for a given channel.
/// </summary>
public interface IEffectiveThemeSettings
{
    ThemeMode Mode { get; }
    Color Accent { get; }
    Color CheckerA { get; }
    Color CheckerB { get; }
    Color OutlineColor { get; }
    Color GuideLine { get; }
    Color GuideText { get; }
    bool IsSystemInDarkMode { get; }
}
