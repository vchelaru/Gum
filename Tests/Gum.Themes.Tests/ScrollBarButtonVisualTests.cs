using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using Editor = Gum.Themes.Editor;
using Retro95 = Gum.Themes.Retro95;

namespace Gum.Themes.Tests;

/// <summary>
/// The themes whose scroll bars keep the up and down arrows restyle them separately from icon
/// buttons: a ScrollBarButton gets the theme's own <c>ScrollBarButtonVisual</c>, a copy of the
/// theme's <c>ButtonVisual</c> (#6165).
/// </summary>
public class ScrollBarButtonVisualTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals, typeof(Editor.ScrollBarButtonVisual), typeof(Editor.ButtonVisual) };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals, typeof(Retro95.ScrollBarButtonVisual), typeof(Retro95.ButtonVisual) };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_ShouldGiveScrollBarButtonTheThemesVisual(
        string themeName, Action configureStyling, Action registerVisuals, Type visualType, Type buttonVisualType)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            ScrollBarButton button = new();

            button.Visual.ShouldBeOfType(visualType, themeName);
            visualType.BaseType.ShouldBe(buttonVisualType, themeName);
            button.Visual.FormsControlAsObject.ShouldBeOfType<ScrollBarButton>(themeName);
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
