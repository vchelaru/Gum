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
}
