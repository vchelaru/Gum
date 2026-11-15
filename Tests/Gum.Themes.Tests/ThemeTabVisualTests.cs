using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;
using V3 = Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Tests;

/// <summary>
/// A code-only theme styles TabControl and TabItem like its tool-content tabs instead of leaving the
/// stock V3 visuals in place (#7936). TabPage has nothing to theme, so each theme pins the stock one.
/// </summary>
public class ThemeTabVisualTests : IDisposable
{
    private readonly Dictionary<Type, VisualTemplate> _previousTemplates = new(FrameworkElement.DefaultFormsTemplates);

    public void Dispose()
    {
        FrameworkElement.DefaultFormsTemplates.Clear();
        foreach (KeyValuePair<Type, VisualTemplate> pair in _previousTemplates)
        {
            FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
        }
    }

    public static IEnumerable<object[]> Themes()
    {
        foreach (object[] row in ThemesWithSelectedFill())
        {
            yield return new[] { row[0], row[1], row[2] };
        }
    }

    // Theme name, configure, register, and the fill a selected tab shows (the theme's selection color).
    public static IEnumerable<object[]> ThemesWithSelectedFill()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals,
            (Func<Color>)(() => Bubblegum.BubblegumStyling.ActiveStyle.Colors.SelectedRow) };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals,
            (Func<Color>)(() => DarkPro.DarkProStyling.ActiveStyle.Colors.Accent) };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals,
            (Func<Color>)(() => Editor.EditorStyling.ActiveStyle.Colors.Selection) };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals,
            (Func<Color>)(() => ForestGlade.ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillTop) };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals,
            (Func<Color>)(() => Hazard.HazardStyling.ActiveStyle.Colors.Accent) };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals,
            (Func<Color>)(() => Meadow.MeadowStyling.ActiveStyle.Colors.Blue) };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals,
            (Func<Color>)(() => Neon.NeonStyling.ActiveStyle.Colors.SelectedTab) };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals,
            (Func<Color>)(() => Retro95.Retro95Styling.ActiveStyle.Colors.Selection) };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_TabItem_UsesThemeVisual(string themeName, Action configureStyling, Action registerVisuals)
    {
        configureStyling();
        registerVisuals();

        new TabItem().Visual.GetType().Namespace.ShouldBe("Gum.Themes." + themeName);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_TabControl_UsesThemeVisualOrStockForEditor(string themeName, Action configureStyling, Action registerVisuals)
    {
        configureStyling();
        registerVisuals();

        Type visualType = new TabControl().Visual.GetType();
        if (themeName == "Editor")
        {
            // Editor's tool tabs use the stock content panel look, so only its TabItem is themed.
            visualType.ShouldBe(typeof(V3.TabControlVisual), themeName);
        }
        else
        {
            visualType.Namespace.ShouldBe("Gum.Themes." + themeName);
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_TabPage_PinsStockVisual(string themeName, Action configureStyling, Action registerVisuals)
    {
        configureStyling();
        registerVisuals();

        new TabPage().Visual.GetType().ShouldBe(typeof(V3.TabPageVisual), themeName);
    }

    [Theory]
    [MemberData(nameof(ThemesWithSelectedFill))]
    public void TabItem_Selected_ShowsThemeSelectionFill(string themeName, Action configureStyling, Action registerVisuals, Func<Color> selectedFill)
    {
        configureStyling();
        registerVisuals();

        TabItem tab = new TabItem { Header = "One" };

        FillColors(tab.Visual).ShouldNotContain(selectedFill(), themeName);

        tab.IsSelected = true;
        FillColors(tab.Visual).ShouldContain(selectedFill(), themeName);

        tab.IsSelected = false;
        FillColors(tab.Visual).ShouldNotContain(selectedFill(), themeName);
    }

    [Theory]
    [MemberData(nameof(ThemesWithSelectedFill))]
    public void TabControl_SelectedTabFollowsSelection(string themeName, Action configureStyling, Action registerVisuals, Func<Color> selectedFill)
    {
        configureStyling();
        registerVisuals();

        TabControl tabControl = new TabControl();
        TabItem first = tabControl.AddTab("One");
        TabItem second = tabControl.AddTab("Two");

        tabControl.SelectedIndex = 1;

        FillColors(first.Visual).ShouldNotContain(selectedFill(), themeName);
        FillColors(second.Visual).ShouldContain(selectedFill(), themeName);
    }

    private static List<Color> FillColors(GraphicalUiElement root)
    {
        List<Color> colors = new();
        Collect(root, colors);
        return colors;
    }

    private static void Collect(GraphicalUiElement element, List<Color> colors)
    {
        if (element is RectangleRuntime rectangle && rectangle.IsFilled)
        {
            colors.Add(rectangle.FillColor);
        }
        else if (element is NineSliceRuntime nineSlice)
        {
            colors.Add(nineSlice.Color);
        }

        foreach (GraphicalUiElement child in element.Children)
        {
            Collect(child, colors);
        }
    }
}
