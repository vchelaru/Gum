using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// ListBox and ScrollViewer of every packaged theme, loaded from the theme files: the items and content stay
/// inside the clip area, the scroll range reaches the last item, the two bars of a ScrollViewer do not
/// overlap, for 0, 1 and many items, tall and short controls, and items of different heights.
/// </summary>
public class ThemedScrollingControlLayoutTests : BeginnerDriverTestBase
{
    private const float Tol = 1.01f;

    public static IEnumerable<object[]> ThemeAndCount() =>
        ThemedControlHarness.ThemeNames.SelectMany(t => new[] { 0, 1, 6, 40 }.Select(c => new object[] { t, c }));

    private (InteractiveGue Box, ListBox List) ListWith(string theme, int count, float width = 250, float height = 200)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        InteractiveGue box = ThemedControlHarness.PlaceAlone(Driver, project, ThemedControlHarness.Find(project, "ListBox")!);
        ListBox list = (ListBox)box.FormsControlAsObject!;
        list.Width = width;
        list.Height = height;
        for (int i = 0; i < count; i++)
        {
            list.Items!.Add("Item " + i);
        }
        Driver.Step(3);
        return (box, list);
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_ScrolledToMaximum_LastItemIsFullyInsideTheClipArea(string theme, int count)
    {
        var s = ListWith(theme, count);
        if (count == 0) { s.List.InnerPanel.Children.Count.ShouldBe(0); return; }
        s.List.VerticalScrollBarValue = s.List.VerticalScrollBarMaximum;
        Driver.Step(2);

        GraphicalUiElement last = s.List.InnerPanel.Children.OfType<GraphicalUiElement>().Last();
        GraphicalUiElement clip = s.List.ClipContainer;
        (last.AbsoluteTop + last.AbsoluteHeight).ShouldBeLessThanOrEqualTo(clip.AbsoluteTop + clip.AbsoluteHeight + Tol,
            $"{theme} {count} items: last item {last.AbsoluteTop}+{last.AbsoluteHeight}, clip {clip.AbsoluteTop}+{clip.AbsoluteHeight}");
        last.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - Tol, $"{theme} {count} items: last item scrolled above the clip area");
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_ScrolledToMinimum_FirstItemStartsAtClipTop(string theme, int count)
    {
        var s = ListWith(theme, count);
        if (count == 0) { return; }
        s.List.VerticalScrollBarValue = 0;
        Driver.Step(2);

        GraphicalUiElement first = s.List.InnerPanel.Children.OfType<GraphicalUiElement>().First();
        first.AbsoluteTop.ShouldBe(s.List.ClipContainer.AbsoluteTop, Tol, $"{theme} {count} items");
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_Items_FitInsideTheListBoxWidth(string theme, int count)
    {
        var s = ListWith(theme, count);
        float right = s.Box.AbsoluteLeft + s.Box.AbsoluteWidth;

        foreach (GraphicalUiElement item in s.List.InnerPanel.Children.OfType<GraphicalUiElement>())
        {
            item.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(s.Box.AbsoluteLeft - Tol, $"{theme} {count} items: {item.Name}");
            (item.AbsoluteLeft + item.AbsoluteWidth).ShouldBeLessThanOrEqualTo(right + Tol, $"{theme} {count} items: {item.Name}");
        }
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_ItemsThatFit_HaveNoScrollRange(string theme, int count)
    {
        var s = ListWith(theme, count, height: 400);
        if (count > 6) { return; }

        s.List.VerticalScrollBarMaximum.ShouldBeLessThanOrEqualTo(0.5, $"{theme} {count} items fit in a 400 tall list");
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_ScrollBarVisibility_FollowsWhetherItemsOverflow(string theme, int count)
    {
        var s = ListWith(theme, count);
        GraphicalUiElement bar = s.Box.FindByName("VerticalScrollBarInstance")!;
        bool overflows = s.List.VerticalScrollBarMaximum > 0.5;

        bar.AbsoluteVisible.ShouldBe(overflows, $"{theme} {count} items");
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_VariableHeightItems_ScrollRangeReachesLastItem(string theme, int count)
    {
        var s = ListWith(theme, count);
        if (count == 0) { return; }
        int index = 0;
        foreach (GraphicalUiElement item in s.List.InnerPanel.Children.OfType<GraphicalUiElement>())
        {
            item.HeightUnits = DimensionUnitType.Absolute;
            item.Height = 20 + (index++ % 3) * 25;
        }
        Driver.Step(3);
        s.List.VerticalScrollBarValue = s.List.VerticalScrollBarMaximum;
        Driver.Step(2);

        GraphicalUiElement last = s.List.InnerPanel.Children.OfType<GraphicalUiElement>().Last();
        GraphicalUiElement clip = s.List.ClipContainer;
        (last.AbsoluteTop + last.AbsoluteHeight).ShouldBeLessThanOrEqualTo(clip.AbsoluteTop + clip.AbsoluteHeight + Tol,
            $"{theme} {count} items: last item {last.AbsoluteTop}+{last.AbsoluteHeight}, clip {clip.AbsoluteTop}+{clip.AbsoluteHeight}");
        if (s.List.VerticalScrollBarMaximum > 0.5)
        {
            (last.AbsoluteTop + last.AbsoluteHeight).ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop + clip.AbsoluteHeight - Tol - 1,
                $"{theme} {count} items: scrolled to the end but a gap remains under the last item");
        }
    }

    [Theory]
    [MemberData(nameof(ThemeAndCount))]
    public void ListBox_ClipArea_StaysInsideTheListBox(string theme, int count)
    {
        var s = ListWith(theme, count, width: 90, height: 60);
        GraphicalUiElement clip = s.List.ClipContainer;

        clip.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(s.Box.AbsoluteLeft - Tol, $"{theme}");
        clip.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(s.Box.AbsoluteTop - Tol, $"{theme}");
        (clip.AbsoluteLeft + clip.AbsoluteWidth).ShouldBeLessThanOrEqualTo(s.Box.AbsoluteLeft + s.Box.AbsoluteWidth + Tol, $"{theme}");
        (clip.AbsoluteTop + clip.AbsoluteHeight).ShouldBeLessThanOrEqualTo(s.Box.AbsoluteTop + s.Box.AbsoluteHeight + Tol, $"{theme}");
    }

    private (InteractiveGue Box, ScrollViewer Viewer) ViewerWith(string theme, float width, float height, float contentWidth, float contentHeight)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        InteractiveGue box = ThemedControlHarness.PlaceAlone(Driver, project, ThemedControlHarness.Find(project, "ScrollViewer")!);
        ScrollViewer viewer = (ScrollViewer)box.FormsControlAsObject!;
        viewer.Width = width;
        viewer.Height = height;
        viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        ColoredRectangleRuntime content = new ColoredRectangleRuntime { Width = contentWidth, Height = contentHeight };
        content.WidthUnits = DimensionUnitType.Absolute;
        content.HeightUnits = DimensionUnitType.Absolute;
        viewer.AddChild(content);
        Driver.Step(3);
        return (box, viewer);
    }

    public static IEnumerable<object[]> ThemeAndViewerSize() =>
        ThemedControlHarness.ThemeNames.SelectMany(t => new[] { 120f, 301f }.Select(w => new object[] { t, w }));

    [Theory]
    [MemberData(nameof(ThemeAndViewerSize))]
    public void ScrollViewer_BothBars_DoNotOverlapEachOther(string theme, float size)
    {
        var s = ViewerWith(theme, size, size - 20, 900, 900);
        GraphicalUiElement? v = s.Box.FindByName("VerticalScrollBarInstance");
        GraphicalUiElement? h = s.Box.FindByName("HorizontalScrollBarInstance");
        if (v == null || h == null || !v.AbsoluteVisible || !h.AbsoluteVisible) { return; }

        bool overlap = v.AbsoluteLeft < h.AbsoluteLeft + h.AbsoluteWidth - Tol && v.AbsoluteLeft + v.AbsoluteWidth > h.AbsoluteLeft + Tol
            && v.AbsoluteTop < h.AbsoluteTop + h.AbsoluteHeight - Tol && v.AbsoluteTop + v.AbsoluteHeight > h.AbsoluteTop + Tol;
        overlap.ShouldBeFalse($"{theme} {size}: vertical {v.AbsoluteLeft},{v.AbsoluteTop} {v.AbsoluteWidth}x{v.AbsoluteHeight}; horizontal {h.AbsoluteLeft},{h.AbsoluteTop} {h.AbsoluteWidth}x{h.AbsoluteHeight}");
    }

    [Theory]
    [MemberData(nameof(ThemeAndViewerSize))]
    public void ScrollViewer_Bars_StayInsideTheViewer(string theme, float size)
    {
        var s = ViewerWith(theme, size, size - 20, 900, 900);
        List<string> outside = new List<string>();
        foreach (string name in new[] { "VerticalScrollBarInstance", "HorizontalScrollBarInstance" })
        {
            GraphicalUiElement? bar = s.Box.FindByName(name);
            if (bar == null || !bar.AbsoluteVisible) { continue; }
            if (bar.AbsoluteLeft < s.Box.AbsoluteLeft - Tol || bar.AbsoluteTop < s.Box.AbsoluteTop - Tol
                || bar.AbsoluteLeft + bar.AbsoluteWidth > s.Box.AbsoluteLeft + s.Box.AbsoluteWidth + Tol
                || bar.AbsoluteTop + bar.AbsoluteHeight > s.Box.AbsoluteTop + s.Box.AbsoluteHeight + Tol)
            {
                outside.Add($"{name} {bar.AbsoluteLeft},{bar.AbsoluteTop} {bar.AbsoluteWidth}x{bar.AbsoluteHeight} vs viewer {s.Box.AbsoluteWidth}x{s.Box.AbsoluteHeight}");
            }
        }
        outside.ShouldBeEmpty($"{theme} {size}: " + string.Join("; ", outside));
    }

    [Theory]
    [MemberData(nameof(ThemeAndViewerSize))]
    public void ScrollViewer_ScrolledToVerticalMaximum_ContentBottomEndsAtTheClipArea(string theme, float size)
    {
        var s = ViewerWith(theme, size, size - 20, 900, 900);
        s.Viewer.VerticalScrollBarValue = s.Viewer.VerticalScrollBarMaximum;
        Driver.Step(2);

        GraphicalUiElement content = s.Viewer.InnerPanel.Children.OfType<GraphicalUiElement>().Last();
        GraphicalUiElement clip = s.Viewer.ClipContainer;
        (content.AbsoluteTop + content.AbsoluteHeight).ShouldBe(clip.AbsoluteTop + clip.AbsoluteHeight, 2f, $"{theme} {size}: bottom");
    }

    [Theory]
    [MemberData(nameof(ThemeAndViewerSize))]
    public void ScrollViewer_SizeToChildrenInnerPanel_ScrollsHorizontallyToTheContentEnd(string theme, float size)
    {
        var s = ViewerWith(theme, size, size - 20, 900, 40);
        s.Viewer.InnerPanel.Dock(Gum.Wireframe.Dock.SizeToChildren);
        Driver.Step(3);

        s.Viewer.HorizontalScrollBarMaximum.ShouldBeGreaterThan(0, $"{theme} {size}: 900 wide content in a {size} wide viewer");
        s.Viewer.HorizontalScrollBarValue = s.Viewer.HorizontalScrollBarMaximum;
        Driver.Step(2);
        GraphicalUiElement content = s.Viewer.InnerPanel.Children.OfType<GraphicalUiElement>().Last();
        (content.AbsoluteLeft + content.AbsoluteWidth).ShouldBe(s.Viewer.ClipContainer.AbsoluteLeft + s.Viewer.ClipContainer.AbsoluteWidth, 2f, $"{theme} {size}: right end");
    }

    [Theory]
    [MemberData(nameof(ThemeAndViewerSize))]
    public void ScrollViewer_ClipArea_DoesNotCoverTheVisibleBars(string theme, float size)
    {
        var s = ViewerWith(theme, size, size - 20, 900, 900);
        GraphicalUiElement clip = s.Viewer.ClipContainer;
        GraphicalUiElement? v = s.Box.FindByName("VerticalScrollBarInstance");
        GraphicalUiElement? h = s.Box.FindByName("HorizontalScrollBarInstance");

        if (v != null && v.AbsoluteVisible)
        {
            (clip.AbsoluteLeft + clip.AbsoluteWidth).ShouldBeLessThanOrEqualTo(v.AbsoluteLeft + Tol, $"{theme} {size}: content area runs under the vertical bar");
        }
        if (h != null && h.AbsoluteVisible)
        {
            (clip.AbsoluteTop + clip.AbsoluteHeight).ShouldBeLessThanOrEqualTo(h.AbsoluteTop + Tol, $"{theme} {size}: content area runs under the horizontal bar");
        }
    }
}
