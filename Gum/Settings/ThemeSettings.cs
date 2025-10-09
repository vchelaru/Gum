using Gum.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Settings;

public class ThemeSettings
{
    public ThemeMode? Mode { get; set; }
    public Color? Accent { get; set; }
    public Color? CheckerA { get; set; }
    public Color? CheckerB { get; set; }
    public Color? OutlineColor { get; set; }
    public Color? GuideLine { get; set; }
    public Color? GuideText { get; set; }

    internal static void MigrateExplicitLegacyColors(GeneralSettingsFile settings, ThemeSettings themeSettings)
    {
        ThemeDefaultsProvider defaults = new();
        Color checker1Old = Color.FromArgb(settings.CheckerColor1R, settings.CheckerColor1G, settings.CheckerColor1B);
        Color checker2Old = Color.FromArgb(settings.CheckerColor2R, settings.CheckerColor2G, settings.CheckerColor2B);
        Color outlineColorOld = Color.FromArgb(settings.OutlineColorR, settings.OutlineColorG, settings.OutlineColorB);
        Color guideLineColorOld = Color.FromArgb(settings.GuideLineColorR, settings.GuideLineColorG, settings.GuideLineColorB);
        Color guideTextColorOld = Color.FromArgb(settings.GuideTextColorR, settings.GuideTextColorG, settings.GuideTextColorB);

        if (checker1Old != defaults.CheckerA(ThemeMode.Light))
        {
            themeSettings.CheckerA = checker1Old;
        }

        if (checker2Old != defaults.CheckerB(ThemeMode.Light))
        {
            themeSettings.CheckerB = checker2Old;
        }

        if (outlineColorOld != defaults.OutlineColor)
        {
            themeSettings.OutlineColor = outlineColorOld;
        }

        if (guideLineColorOld != defaults.GuideLine)
        {
            themeSettings.GuideLine = guideLineColorOld;
        }

        if (guideTextColorOld != defaults.GuideText)
        {
            themeSettings.GuideText = guideTextColorOld;
        }
    }
}

public sealed class ThemeDefaultsProvider
{
    public ThemeMode Mode => ThemeMode.System;
    public Color Accent => Color.FromArgb(108, 195, 149);

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