using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using V3 = Gum.Forms.DefaultVisuals.V3;
using Bubblegum = Gum.Themes.Bubblegum;
using DarkPro = Gum.Themes.DarkPro;
using Editor = Gum.Themes.Editor;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Neon = Gum.Themes.Neon;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;

namespace Gum.Themes.Tests;

/// <summary>
/// Every theme gives ProgressBar its own visual, built on the V3 ProgressBarVisual, and the
/// ProgressBar still sizes that visual's indicator from Value.
/// </summary>
public class ProgressBarVisualTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals, typeof(Bubblegum.ProgressBarVisual) };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals, typeof(DarkPro.ProgressBarVisual) };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals, typeof(Editor.ProgressBarVisual) };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals, typeof(ForestGlade.ProgressBarVisual) };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals, typeof(Hazard.ProgressBarVisual) };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals, typeof(Meadow.ProgressBarVisual) };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals, typeof(Neon.ProgressBarVisual) };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals, typeof(Retro95.ProgressBarVisual) };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals, typeof(Template.ProgressBarVisual) };
    }

    public static IEnumerable<object[]> TrackNames()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals, "TrackBackground" };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals, "ProgressBarTrack" };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals, "Retro95BevelFill" };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals, "ProgressBarTrack" };
    }

    [Theory]
    [MemberData(nameof(TrackNames))]
    public void Colors_SetThroughBaseVisualType_ShouldColorTheDrawnTrackAndIndicator(
        string themeName, Action configureStyling, Action registerVisuals, string trackName)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            ProgressBar progressBar = new();
            V3.ProgressBarVisual visual = (V3.ProgressBarVisual)progressBar.Visual;
            visual.TrackColor = new Color(1, 2, 3);
            visual.IndicatorColor = new Color(4, 5, 6);

            DrawnColor(visual, trackName).ShouldBe(new Color(1, 2, 3), themeName);
            DrawnColor(visual, "IndicatorInstance").ShouldBe(new Color(4, 5, 6), themeName);
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

    // Shapes draw with FillColor, NineSlices with Color.
    private static Color DrawnColor(GraphicalUiElement visual, string childName)
    {
        GraphicalUiElement child = visual.GetGraphicalUiElementByName(childName)!;
        System.Reflection.PropertyInfo property = child.GetType().GetProperty("FillColor") ?? child.GetType().GetProperty("Color")!;
        return (Color)property.GetValue(child)!;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_ShouldGiveProgressBarTheThemesVisual(
        string themeName, Action configureStyling, Action registerVisuals, Type progressBarVisualType)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            ProgressBar progressBar = new();
            progressBar.Value = 35;

            progressBar.Visual.ShouldBeOfType(progressBarVisualType, themeName);
            progressBarVisualType.BaseType.ShouldBe(typeof(Gum.Forms.DefaultVisuals.V3.ProgressBarVisual), themeName);
            GraphicalUiElement indicator = progressBar.Visual.GetGraphicalUiElementByName("IndicatorInstance")!;
            indicator.Width.ShouldBe(35, themeName);
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
