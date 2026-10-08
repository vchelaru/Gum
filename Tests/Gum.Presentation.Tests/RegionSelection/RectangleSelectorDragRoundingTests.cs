using Gum.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using ResizeSide = TextureCoordinateSelectionPlugin.RegionSelection.ResizeSide;
// Aliased under a distinct name: this test namespace nests under "Gum", which also declares its own
// unrelated RectangleSelector (the scene drag-select box). That enclosing-namespace declaration wins
// over an unqualified "RectangleSelector" even with a using-alias of the same name in this file, so
// this alias needs its own name to actually bind to the texture-coordinate selector below.
using TexCoordRectangleSelector = TextureCoordinateSelectionPlugin.RegionSelection.RectangleSelector;

namespace Gum.Presentation.Tests.RegionSelection;

/// <summary>
/// Pins that the whole-pixel rounding (RoundToUnitCoordinates) keeps applying to the live display
/// while a drag is in progress and no grid snapping is configured. Without this, the selector shows
/// its raw sub-pixel drag position while ControlLogic.HandleRegionChanged commits a pixel-rounded
/// TextureLeft/Top/Width/Height on every drag tick, so the on-screen box visibly disagrees with (jitters
/// against) the value it just committed. Issue #4763.
/// </summary>
public class RectangleSelectorDragRoundingTests
{
    private static TexCoordRectangleSelector CreateSelector()
    {
        var managers = new SystemManagers
        {
            Renderer = new Renderer()
        };

        return new TexCoordRectangleSelector(managers, new CanvasDisplayScale())
        {
            RoundToUnitCoordinates = true,
            SnappingGridSize = null
        };
    }

    [Fact]
    public void LeftTop_WhileMiddleIsGrabbed_ShouldRoundToWholePixelWhenNoGridIsSet()
    {
        var selector = CreateSelector();
        selector.SideGrabbed = ResizeSide.Middle;

        selector.Left = 13.4f;
        selector.Top = 7.6f;

        selector.Left.ShouldBe(13f);
        selector.Top.ShouldBe(8f);
    }

    [Fact]
    public void WidthHeight_WhileResizeSideIsGrabbed_ShouldRoundToWholePixelWhenNoGridIsSet()
    {
        var selector = CreateSelector();
        selector.SideGrabbed = ResizeSide.BottomRight;

        selector.Width = 27.4f;
        selector.Height = 41.6f;

        selector.Width.ShouldBe(27f);
        selector.Height.ShouldBe(42f);
    }

    [Theory]
    [InlineData(ResizeSide.Left)]
    [InlineData(ResizeSide.TopLeft)]
    public void Right_WhileLeftEdgeIsDraggedWithGridSnapping_ShouldStayFixed(ResizeSide sideGrabbed)
    {
        var selector = CreateSelector();
        selector.SnappingGridSize = 16;
        selector.Left = 96f;
        selector.Width = 48f;
        selector.Right.ShouldBe(144f);

        // Dragging the left edge 9px right: Left moves, Width shrinks by the same amount.
        selector.SideGrabbed = sideGrabbed;
        selector.Left = 105f;
        selector.Width = 39f;

        selector.Right.ShouldBe(144f);
    }

    [Fact]
    public void LeftEdgeDrag_WithGridSnapping_ShouldSnapLeftAndKeepRightFixedAfterRelease()
    {
        var selector = CreateSelector();
        selector.SnappingGridSize = 16;
        selector.Left = 96f;
        selector.Width = 48f;

        selector.SideGrabbed = ResizeSide.Left;
        selector.Left = 105f;
        selector.Width = 39f;
        selector.SideGrabbed = ResizeSide.None;
        selector.ApplyGridSnappingOnRelease(ResizeSide.Left);

        selector.Left.ShouldBe(112f);
        selector.Right.ShouldBe(144f);
    }

    [Theory]
    [InlineData(ResizeSide.Top)]
    [InlineData(ResizeSide.TopLeft)]
    public void Bottom_WhileTopEdgeIsDraggedWithGridSnapping_ShouldStayFixed(ResizeSide sideGrabbed)
    {
        var selector = CreateSelector();
        selector.SnappingGridSize = 16;
        selector.Top = 96f;
        selector.Height = 48f;
        selector.Bottom.ShouldBe(144f);

        selector.SideGrabbed = sideGrabbed;
        selector.Top = 105f;
        selector.Height = 39f;

        selector.Bottom.ShouldBe(144f);
    }
}
