using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Shouldly;
using V3MenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.Tests;

/// <summary>
/// Every theme's Menu shows a focus bar under itself while focused, colored the way that theme
/// colors its ListBox focus (#6706).
/// </summary>
public class MenuFocusIndicatorTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals, (Func<Color>)(() => Bubblegum.BubblegumStyling.ActiveStyle.Colors.FocusRing) };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals, (Func<Color>)(() => DarkPro.DarkProStyling.ActiveStyle.Colors.Accent) };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals, (Func<Color>)(() => ForestGlade.ForestGladeStyling.ActiveStyle.Colors.AccentHalo) };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals, (Func<Color>)(() => Hazard.HazardStyling.ActiveStyle.Colors.Accent) };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals, (Func<Color>)(() => Meadow.MeadowStyling.ActiveStyle.Colors.SageDark) };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals, (Func<Color>)(() => Neon.NeonStyling.ActiveStyle.Colors.FocusRing) };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals, (Func<Color>)(() => Retro95.Retro95Styling.ActiveStyle.Colors.Text) };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals, (Func<Color>)(() => Template.TemplateStyling.ActiveStyle.Colors.Accent) };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void IsFocused_ShouldShowFocusBarInThemeFocusColor(
        string themeName, Action configureStyling, Action registerVisuals, Func<Color> expectedFocusColor)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            Menu menu = new();
            V3MenuVisual visual = menu.Visual.ShouldBeAssignableTo<V3MenuVisual>(themeName)!;

            visual.FocusedIndicator.Parent.ShouldBe(visual, themeName);
            visual.FocusedIndicator.Visible.ShouldBeFalse(themeName);

            menu.IsFocused = true;

            visual.FocusedIndicator.Visible.ShouldBeTrue(themeName);
            visual.FocusedIndicatorColor.ShouldBe(expectedFocusColor(), themeName);
            visual.FocusedIndicator.Color.ShouldBe(expectedFocusColor(), themeName);

            menu.IsFocused = false;

            visual.FocusedIndicator.Visible.ShouldBeFalse(themeName);
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
        }
    }
}
