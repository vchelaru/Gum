using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using Bubblegum = Gum.Themes.Bubblegum;
using Editor = Gum.Themes.Editor;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;

namespace Gum.Themes.Tests;

/// <summary>
/// Every theme restyles slider and scroll bar thumbs separately from buttons: a Thumb gets the
/// theme's own <c>ThumbVisual</c>, a copy of the theme's <c>ButtonVisual</c> (#6165).
/// </summary>
public class ThumbVisualTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals, typeof(Bubblegum.ThumbVisual), typeof(Bubblegum.ButtonVisual) };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals, typeof(Editor.ThumbVisual), typeof(Editor.ButtonVisual) };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals, typeof(ForestGlade.ThumbVisual), typeof(ForestGlade.ButtonVisual) };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals, typeof(Hazard.ThumbVisual), typeof(Hazard.ButtonVisual) };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals, typeof(Meadow.ThumbVisual), typeof(Meadow.ButtonVisual) };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals, typeof(Retro95.ThumbVisual), typeof(Retro95.ButtonVisual) };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals, typeof(Template.ThumbVisual), typeof(Template.ButtonVisual) };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_ShouldGiveThumbTheThemesThumbVisual(
        string themeName, Action configureStyling, Action registerVisuals, Type thumbVisualType, Type buttonVisualType)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            Thumb thumb = new();

            thumb.Visual.ShouldBeOfType(thumbVisualType, themeName);
            thumbVisualType.BaseType.ShouldBe(buttonVisualType, themeName);
            thumb.Visual.FormsControlAsObject.ShouldBeOfType<Thumb>(themeName);
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
