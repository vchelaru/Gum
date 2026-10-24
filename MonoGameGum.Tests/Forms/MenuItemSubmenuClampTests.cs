using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A submenu taller than the canvas is moved to the top, shrunk to the canvas height and scrolls; it
/// goes back to sizing itself to its rows once it fits again (#6758).
/// </summary>
public class MenuItemSubmenuClampTests : BaseTestClass
{
    private static MenuItem CreateMenu(float canvasWidth, float canvasHeight, float x = 0)
    {
        ResizeCanvas(canvasWidth, canvasHeight);
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
        return file;
    }

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static void Tick()
    {
        Gum.GumService.Default.Root.AnimateSelf(0.016);
        FrameworkElement.PopupRoot.AnimateSelf(0.016);
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

    private static void AddRows(MenuItem owner, int count)
    {
        for (int i = 0; i < count; i++)
        {
            owner.Items!.Add("Item " + i);
        }
    }

    // The popup fills the canvas height, its rows are not cut off at the side, and scrolling to the end
    // shows the last row inside the clipped area.
    private static void ShouldBeClampedAndScrollable(ScrollViewer popup, float canvasWidth, float canvasHeight)
    {
        popup.Visual.AbsoluteTop.ShouldBe(0, 0.01f);
        popup.Visual.AbsoluteBottom.ShouldBe(canvasHeight, 0.01f);
        popup.Visual.AbsoluteRight.ShouldBeLessThanOrEqualTo(canvasWidth + 0.01f);
        popup.VerticalScrollBar!.IsVisible.ShouldBeTrue();

        GraphicalUiElement clip = (GraphicalUiElement)popup.InnerPanel.Parent!;
        clip.AbsoluteWidth.ShouldBeGreaterThanOrEqualTo(popup.InnerPanel.AbsoluteWidth - 0.01f);
        clip.AbsoluteBottom.ShouldBeLessThanOrEqualTo(canvasHeight + 0.01f);

        popup.VerticalScrollBarValue = popup.VerticalScrollBarMaximum;
        popup.VerticalScrollBarMaximum.ShouldBeGreaterThan(0);
        GraphicalUiElement lastRow = popup.InnerPanel.Children![^1];
        lastRow.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(clip.AbsoluteBottom + 0.01f);
        lastRow.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f);
    }

    private static void ShouldBeSizedToRows(ScrollViewer popup)
    {
        popup.Visual.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
        popup.Visual.WidthUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
        popup.VerticalScrollBar!.IsVisible.ShouldBeFalse();
        popup.VerticalScrollBarValue.ShouldBe(0);
        popup.Visual.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(popup.InnerPanel.AbsoluteHeight);
    }

    [Fact]
    public void Open_ShouldClampToCanvasAndScroll_WhenSubmenuIsTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 200);

        file.IsSelected = true;

        ShouldBeClampedAndScrollable(PopupOf(file), 800, 600);
    }

    [Fact]
    public void ItemsAdd_ShouldClampToCanvasAndScroll_WhenAppendsMakeTheOpenSubmenuTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        file.Items!.Add("Start");
        file.IsSelected = true;

        AddRows(file, 200);

        ShouldBeClampedAndScrollable(PopupOf(file), 800, 600);
    }

    [Fact]
    public void Open_ShouldClampToCanvasAndScroll_WhenNestedSubmenuIsTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        MenuItem recent = new() { Header = "Recent" };
        file.Items!.Add(recent);
        AddRows(recent, 200);
        file.IsSelected = true;

        recent.IsSelected = true;

        ShouldBeClampedAndScrollable(PopupOf(recent), 800, 600);
        PopupOf(recent).Visual.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(PopupOf(file).Visual.AbsoluteLeft);
    }

    [Fact]
    public void Tick_ShouldClampOpenSubmenu_WhenCanvasShrinksBelowItsHeight()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        naturalHeight.ShouldBeLessThan(600);

        ResizeCanvas(800, naturalHeight / 2);
        Tick();

        ShouldBeClampedAndScrollable(popup, 800, naturalHeight / 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tick_ShouldKeepOpenSubmenuInsideCanvas_WhenCanvasNarrows(bool appendAfterResize)
    {
        MenuItem file = CreateMenu(800, 600, x: 300);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float width = popup.Visual.AbsoluteWidth;
        popup.Visual.AbsoluteLeft.ShouldBe(300, 0.01f);

        ResizeCanvas(300 + width / 2, 600);
        Tick();
        if (appendAfterResize)
        {
            file.Items!.Add("Item 0");
        }

        popup.Visual.AbsoluteRight.ShouldBe(300 + width / 2, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(width, 0.01f);
    }

    [Fact]
    public void Tick_ShouldSizeSubmenuToRowsAgain_WhenCanvasGrowsPastItsHeight()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        float naturalWidth = popup.Visual.AbsoluteWidth;
        ResizeCanvas(800, naturalHeight / 2);
        Tick();

        ResizeCanvas(800, 600);
        Tick();

        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBe(naturalHeight, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(naturalWidth, 0.01f);
    }

    [Fact]
    public void ItemsRemove_ShouldSizeSubmenuToRowsAgain_WhenRemovalsMakeItFit()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        float naturalWidth = popup.Visual.AbsoluteWidth;
        AddRows(file, 200);

        while (file.Items!.Count > 10)
        {
            file.Items.RemoveAt(file.Items.Count - 1);
        }

        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBe(naturalHeight, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(naturalWidth, 0.01f);
    }

    [Fact]
    public void Open_ShouldSizeSubmenuToRows_WhenItFitsTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);

        file.IsSelected = true;

        ScrollViewer popup = PopupOf(file);
        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBeLessThan(600);
        popup.VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Hidden);
    }
}
