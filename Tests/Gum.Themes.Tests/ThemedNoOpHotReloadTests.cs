using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
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
/// A hot reload of a project that holds none of a code-created control's elements changes nothing about the control,
/// in every packaged (code-only) theme. #8131, #8130
/// </summary>
public class ThemedNoOpHotReloadTests
{
    public static IEnumerable<object[]> Themes() => MenuFocusTests.Themes();

    private static void WithTheme(Action configure, Action register, Action body)
    {
        Dictionary<Type, VisualTemplate> previous = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configure();
            register();
            body();
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (var pair in previous) { FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value; }
        }
    }

    private static void NoOpReload(ContainerRuntime host)
    {
        GumHotReloadManager.ApplyDiff(new[] { (GraphicalUiElement)host }, new GumProjectSave(), SystemManagers.Default);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ApplyDiff_NoOp_ShouldKeepScrollViewerVerticalBarSize(string theme, Action configure, Action register)
    {
        WithTheme(configure, register, () =>
        {
            ContainerRuntime host = new() { Width = 300, Height = 300, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            ScrollViewer viewer = new();
            viewer.Visual.Width = 150;
            viewer.Visual.Height = 120;
            viewer.Visual.WidthUnits = DimensionUnitType.Absolute;
            viewer.Visual.HeightUnits = DimensionUnitType.Absolute;
            host.AddChild(viewer.Visual);
            for (int i = 0; i < 30; i++) { viewer.AddChild(new Button { Width = 400 }); }
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            host.UpdateLayout();

            GraphicalUiElement bar = viewer.VerticalScrollBar!.Visual;
            GraphicalUiElement thumb = bar.GetGraphicalUiElementByName("ThumbInstance")!;
            DimensionUnitType unitsBefore = bar.HeightUnits;
            float heightBefore = bar.Height;
            float absoluteBefore = bar.GetAbsoluteHeight();
            float thumbBefore = thumb.GetAbsoluteHeight();

            NoOpReload(host);
            host.UpdateLayout();

            bar.HeightUnits.ShouldBe(unitsBefore, theme);
            bar.Height.ShouldBe(heightBefore, theme);
            bar.GetAbsoluteHeight().ShouldBe(absoluteBefore, theme);
            thumb.GetAbsoluteHeight().ShouldBe(thumbBefore, theme);
        });
    }
}
