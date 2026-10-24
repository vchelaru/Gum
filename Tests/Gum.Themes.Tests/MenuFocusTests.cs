using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Themes.Tests;

/// <summary>
/// Like WPF, a focused Menu shows focus only through its highlighted top-level item: no theme's
/// Menu changes anything on itself when focused (#6706).
/// </summary>
public class MenuFocusTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void IsFocused_ShouldChangeNothingOnTheMenu_AndHighlightTheFirstItem(
        string themeName, Action configureStyling, Action registerVisuals)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            Menu menu = new();
            MenuItem file = new() { Header = "File" };
            MenuItem edit = new() { Header = "Edit" };
            menu.Items.Add(file);
            menu.Items.Add(edit);
            string unfocused = MenuChromeVisibility(menu.Visual);

            menu.IsFocused = true;

            MenuChromeVisibility(menu.Visual).ShouldBe(unfocused, themeName);
            file.IsHighlighted.ShouldBeTrue(themeName);
            edit.IsHighlighted.ShouldBeFalse(themeName);
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

    // The visibility of every element of the menu's own chrome, leaving out its items.
    private static string MenuChromeVisibility(GraphicalUiElement menuVisual)
    {
        List<string> parts = new();
        void Walk(GraphicalUiElement element)
        {
            foreach (GraphicalUiElement child in element.Children!)
            {
                if ((child as InteractiveGue)?.FormsControlAsObject is MenuItem)
                {
                    continue;
                }
                parts.Add($"{child.Name}={child.Visible}");
                Walk(child);
            }
        }
        Walk(menuVisual);
        return string.Join(", ", parts);
    }
}
