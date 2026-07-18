using Gum.Dialogs;
using System.Drawing;

namespace Gum.Settings;

/// <summary>
/// Persisted (headless, ADR-0004) theme color overrides. Split from the legacy migration helper
/// (<c>Gum.Settings.ThemeSettingsMigration</c>, Gum tool project) so this POCO can live in the
/// headless Gum.Presentation assembly (ADR-0005) - the migration itself reads the WinForms-entangled
/// <c>GeneralSettingsFile</c> and can't move with it.
/// </summary>
public class ThemeSettings
{
    public ThemeMode? Mode { get; set; }
    public Color? Accent { get; set; }
    public Color? CheckerA { get; set; }
    public Color? CheckerB { get; set; }
    public Color? OutlineColor { get; set; }
    public Color? GuideLine { get; set; }
    public Color? GuideText { get; set; }
}

public sealed class ThemeDefaultsProvider
{
    public ThemeMode Mode => ThemeMode.System;
    public Color Accent => Color.FromArgb(85, 161, 121);

    public Color CheckerA(ThemeMode mode) => mode == ThemeMode.Dark
        ? Color.FromArgb(255, 30, 30, 30)
        : Color.FromArgb(255, 150, 150, 150);

    public Color CheckerB(ThemeMode mode) => mode == ThemeMode.Dark
        ? Color.FromArgb(255, 40, 40, 40)
        : Color.FromArgb(255, 170, 170, 170);

    public Color OutlineColor => Color.FromArgb(255, 255, 255, 255);
    public Color GuideLine => Color.FromArgb(255, 255, 255, 255);
    public Color GuideText => Color.FromArgb(255, 255, 255, 255);
}
