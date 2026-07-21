using Gum.Dialogs;
using System.Drawing;

namespace Gum.Settings;

/// <summary>
/// One-time migration of legacy per-channel theme colors (<see cref="GeneralSettingsFile"/>) into
/// the newer <see cref="ThemeSettings"/> shape (ADR-0005).
/// </summary>
public static class ThemeSettingsMigration
{
    public static void MigrateExplicitLegacyColors(GeneralSettingsFile settings, ThemeSettings themeSettings)
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
