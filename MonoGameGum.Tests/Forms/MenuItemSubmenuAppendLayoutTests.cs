using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Appending to the Items of an open submenu lays out only the appended rows, and leaves the popup
/// as it would be had it opened with those items (#6702).
/// </summary>
public class MenuItemSubmenuAppendLayoutTests : BaseTestClass
{
    private static (ContainerRuntime container, MenuItem file) Create(float x = 0)
    {
        ContainerRuntime container = new()
        {
            X = x,
            Width = 400,
            WidthUnits = DimensionUnitType.Absolute,
            Height = 40,
            HeightUnits = DimensionUnitType.Absolute
        };
        Gum.GumService.Default.Root.Children!.Add(container);
        Menu menu = new();
        container.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        container.UpdateLayout();
        return (container, file);
    }

    private static ScrollViewer PopupOf(MenuItem owner)
    {
        foreach (GraphicalUiElement popup in FrameworkElement.PopupRoot.Children!)
        {
            if (popup is InteractiveGue gue && gue.FormsControlAsObject is ScrollViewer sv &&
                sv.InnerPanel.Children!.Count > 0 &&
                (sv.InnerPanel.Children[0] as InteractiveGue)?.FormsControlAsObject is MenuItem m &&
                m.ParentMenuItem == owner)
            {
                return sv;
            }
        }
        throw new System.InvalidOperationException("No open popup for " + owner.Header);
    }

    private static List<(float x, float y, float w, float h)> RowRects(MenuItem owner, float originX)
    {
        ScrollViewer popup = PopupOf(owner);
        List<(float, float, float, float)> rects = new()
        {
            (popup.Visual.AbsoluteLeft - originX, popup.Visual.AbsoluteTop, popup.Visual.AbsoluteWidth, popup.Visual.AbsoluteHeight)
        };
        foreach (GraphicalUiElement row in popup.InnerPanel.Children!)
        {
            rects.Add((row.AbsoluteLeft - originX, row.AbsoluteTop, row.AbsoluteWidth, row.AbsoluteHeight));
        }
        return rects;
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void ItemsAdd_ShouldLayOutOnlyTheAppendedRows_WhenSubmenuIsOpen(int rowCount)
    {
        (_, MenuItem file) = Create();
        file.Items!.Add(new MenuItem { Header = "Item 0" });
        file.IsSelected = true;
        for (int i = 1; i < rowCount; i++)
        {
            file.Items.Add(new MenuItem { Header = "Item " + i });
        }

        int before = GraphicalUiElement.UpdateLayoutCallCount;
        file.Items.Add(new MenuItem { Header = "Last" });
        int calls = GraphicalUiElement.UpdateLayoutCallCount - before;

        // About 131 at every row count; it was 1,364 at 100 rows when sizing and placing the popup laid
        // out every row again (#6757), and 2,964 before that on the full re-layout (#6702).
        calls.ShouldBeLessThan(200);
    }

    [Fact]
    public void ItemsAdd_ShouldMatchPopupOpenedWithSameItems_WhenAppendedRowsAreWiderAndMixed()
    {
        string[] headers = { "A", "Bb", "A much wider header than the others", "C", "Dd" };

        (ContainerRuntime appendedContainer, MenuItem appended) = Create(x: 0);
        appended.Items!.Add("Start");
        appended.IsSelected = true;
        foreach (string header in headers)
        {
            appended.Items.Add(header);
            appended.Items.Add(new MenuItem { Header = header + " control" });
        }
        var appendedRects = RowRects(appended, appendedContainer.AbsoluteLeft);
        appended.IsSelected = false;

        (ContainerRuntime freshContainer, MenuItem fresh) = Create(x: 0);
        fresh.Items!.Add("Start");
        foreach (string header in headers)
        {
            fresh.Items.Add(header);
            fresh.Items.Add(new MenuItem { Header = header + " control" });
        }
        fresh.IsSelected = true;
        var freshRects = RowRects(fresh, freshContainer.AbsoluteLeft);

        appendedRects.Count.ShouldBe(freshRects.Count);
        for (int i = 0; i < freshRects.Count; i++)
        {
            appendedRects[i].x.ShouldBe(freshRects[i].x, 0.01f, $"rect {i}");
            appendedRects[i].y.ShouldBe(freshRects[i].y, 0.01f, $"rect {i}");
            appendedRects[i].w.ShouldBe(freshRects[i].w, 0.01f, $"rect {i}");
            appendedRects[i].h.ShouldBe(freshRects[i].h, 0.01f, $"rect {i}");
        }
    }

    [Fact]
    public void ItemsAdd_ShouldKeepNestedSubmenuAndHighlight_WhenAppendingToParentSubmenu()
    {
        (_, MenuItem file) = Create();
        MenuItem recent = new() { Header = "Recent" };
        recent.Items!.Add("one.gumx");
        file.Items!.Add(recent);
        file.IsSelected = true;
        recent.IsSelected = true;
        recent.IsHighlighted = true;

        for (int i = 0; i < 10; i++)
        {
            file.Items.Add("Item " + i);
        }

        recent.IsHighlighted.ShouldBeTrue();
        recent.IsPopupVisible.ShouldBeTrue();
        ScrollViewer nested = PopupOf(recent);
        nested.Visual.AbsoluteLeft.ShouldBe(recent.Visual.GetAbsoluteRight(), 0.01f);
        nested.Visual.AbsoluteTop.ShouldBe(recent.Visual.AbsoluteTop, 0.01f);
        List<GraphicalUiElement> roots = new(FrameworkElement.PopupRoot.Children!);
        roots.IndexOf(nested.Visual).ShouldBeGreaterThan(roots.IndexOf(PopupOf(file).Visual));
    }

    [Fact]
    public void ItemsAdd_ShouldKeepPopupOnScreenTopAndRowsStacked_WhenAppendsMakeItTallerThanTheCanvas()
    {
        (_, MenuItem file) = Create();
        file.Items!.Add("Start");
        file.IsSelected = true;
        for (int i = 0; i < 200; i++)
        {
            file.Items.Add("Item " + i);
        }

        ScrollViewer popup = PopupOf(file);
        // A popup taller than the canvas is not shrunk; it is kept from going above the top.
        popup.Visual.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(0);
        List<GraphicalUiElement> rows = new(popup.InnerPanel.Children!);
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].GetAbsoluteTop().ShouldBe(rows[i - 1].GetAbsoluteBottom(), 0.01f);
        }
        popup.InnerPanel.AbsoluteHeight.ShouldBe(rows[^1].GetAbsoluteBottom() - rows[0].GetAbsoluteTop(), 0.01f);
        popup.Visual.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(popup.InnerPanel.AbsoluteHeight);
    }
}
