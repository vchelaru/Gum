using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// A long list in every tool theme, for every control that scrolls: the vertical scroll bar spans the
/// control and its thumb travels from the control's top to its bottom as the list scrolls (the thumb
/// used to sit around the middle of a bar that was a fixed 128 px tall or 0 px tall). #8106
/// </summary>
public class ThemedScrollThumbTravelTests : FileThemeInputTestBase
{
    private const float ControlHeight = 150;
    private const float MaxEdgeGap = 8;

    public static IEnumerable<object[]> ThemeAndControl() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(theme =>
            new[] { "ListBox", "ItemsControl", "ScrollViewer" }.Select(control => new object[] { theme, control }));

    private ScrollViewer LongList(string theme, string control)
    {
        GumProjectSave project = LoadTheme(theme);
        ScrollViewer viewer = Place<ScrollViewer>(project, control);
        viewer.Visual.WidthUnits = DimensionUnitType.Absolute;
        viewer.Visual.HeightUnits = DimensionUnitType.Absolute;
        viewer.Visual.Width = 200;
        viewer.Visual.Height = ControlHeight;
        if (viewer is ItemsControl items)
        {
            for (int i = 0; i < 500; i++) { items.Items!.Add("Item " + i); }
        }
        else
        {
            for (int i = 0; i < 500; i++) { viewer.AddChild(new Label { Text = "Item " + i }); }
        }
        Driver.Step(3);
        return viewer;
    }

    // The arrow buttons of the themes that keep them sit between the bar's end and the thumb.
    private static float ButtonSize(ScrollViewer viewer, string name)
    {
        GraphicalUiElement? button = viewer.VerticalScrollBar!.Visual.GetGraphicalUiElementByName(name);
        return button != null && button.AbsoluteVisible ? button.AbsoluteHeight : 0;
    }

    private static (GraphicalUiElement Bar, GraphicalUiElement Thumb) Parts(ScrollViewer viewer)
    {
        GraphicalUiElement bar = viewer.VerticalScrollBar!.Visual;
        return (bar, bar.GetGraphicalUiElementByName("ThumbInstance")!);
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void VerticalScrollBar_ShouldSpanTheControl(string theme, string control)
    {
        ScrollViewer viewer = LongList(theme, control);
        (GraphicalUiElement bar, _) = Parts(viewer);

        bar.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(viewer.Visual.AbsoluteHeight - 2 * MaxEdgeGap, $"{theme} {control}: bar {bar.AbsoluteHeight} in a {viewer.Visual.AbsoluteHeight} control");
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void Thumb_AtTopAndBottomOfTheList_ShouldReachTheControlEdges(string theme, string control)
    {
        ScrollViewer viewer = LongList(theme, control);
        (_, GraphicalUiElement thumb) = Parts(viewer);
        float top = viewer.Visual.AbsoluteTop;
        float bottom = top + viewer.Visual.AbsoluteHeight;

        viewer.VerticalScrollBarValue = 0;
        Driver.Step(2);
        (thumb.AbsoluteTop - top).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(viewer, "UpButtonInstance"), $"{theme} {control}: thumb at the top of the list");

        viewer.VerticalScrollBarValue = viewer.VerticalScrollBarMaximum;
        Driver.Step(2);
        (bottom - (thumb.AbsoluteTop + thumb.AbsoluteHeight)).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(viewer, "DownButtonInstance"), $"{theme} {control}: thumb at the bottom of the list");
        thumb.AbsoluteHeight.ShouldBeGreaterThan(0, $"{theme} {control}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void HorizontalScrollBar_ShouldSpanTheScrollViewer(string theme)
    {
        const string control = "ScrollViewer";
        ScrollViewer viewer = LongList(theme, control);
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        Panel wide = new Panel();
        wide.Visual.WidthUnits = DimensionUnitType.Absolute;
        wide.Visual.Width = 3000;
        wide.Visual.HeightUnits = DimensionUnitType.Absolute;
        wide.Visual.Height = 20;
        viewer.AddChild(wide);
        Driver.Step(3);
        GraphicalUiElement bar = viewer.HorizontalScrollBar!.Visual;

        bar.AbsoluteVisible.ShouldBeTrue($"{theme} {control}");
        bar.AbsoluteWidth.ShouldBeGreaterThanOrEqualTo(viewer.Visual.AbsoluteWidth - 2 * 30 - MaxEdgeGap, $"{theme} {control}: bar {bar.AbsoluteWidth} in a {viewer.Visual.AbsoluteWidth} control");
    }

    [Fact]
    public void EveryShippedThemeFolder_ShouldBeInTheThemeList()
    {
        // A theme missing from AllThemes() is silently untested by every theme test.
        string? dir = System.AppContext.BaseDirectory;
        while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir, "GumFull.sln")))
        {
            dir = System.IO.Path.GetDirectoryName(dir);
        }
        string[] onDisk = System.IO.Directory
            .GetDirectories(System.IO.Path.Combine(dir!, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes"))
            .Select(System.IO.Path.GetFileName).Select(name => name!).ToArray();

        AllThemes().Select(row => (string)row[0]).Where(name => name.Length > 0).OrderBy(name => name)
            .ShouldBe(onDisk.OrderBy(name => name));
    }
}
