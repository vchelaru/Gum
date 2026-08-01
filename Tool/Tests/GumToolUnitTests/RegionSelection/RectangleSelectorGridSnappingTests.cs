using FlatRedBall.SpecializedXnaControls.RegionSelection;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace GumToolUnitTests.RegionSelection;

public class RectangleSelectorGridSnappingTests
{
    // RectangleSelector only needs Renderer.Camera (for handle-position math), so build
    // a bare SystemManagers instead of the full SystemManagers.Initialize, which requires
    // a real GraphicsDevice for its SpriteRenderer.
    private static RectangleSelector CreateSelector()
    {
        var managers = new SystemManagers
        {
            Renderer = new Renderer()
        };

        return new RectangleSelector(managers)
        {
            RoundToUnitCoordinates = false,
            SnappingGridSize = 16
        };
    }

    [Fact]
    public void ApplyGridSnappingOnRelease_WithMiddleGrabbed_ShouldSnapOnlyPositionToGrid()
    {
        var selector = CreateSelector();
        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.ApplyGridSnappingOnRelease(ResizeSide.Middle);

        selector.Left.ShouldBe(16f);
        selector.Top.ShouldBe(0f);
        selector.Width.ShouldBe(27f);
        selector.Height.ShouldBe(41f);
    }

    [Fact]
    public void ApplyGridSnappingOnRelease_WithNoSideGrabbed_ShouldNotSnapAnything()
    {
        var selector = CreateSelector();
        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.ApplyGridSnappingOnRelease(ResizeSide.None);

        selector.Left.ShouldBe(13f);
        selector.Top.ShouldBe(7f);
        selector.Width.ShouldBe(27f);
        selector.Height.ShouldBe(41f);
    }

    [Fact]
    public void ApplyGridSnappingOnRelease_WithResizeSideGrabbed_ShouldSnapOnlySizeToGrid()
    {
        var selector = CreateSelector();
        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.ApplyGridSnappingOnRelease(ResizeSide.BottomRight);

        selector.Left.ShouldBe(13f);
        selector.Top.ShouldBe(7f);
        selector.Width.ShouldBe(32f);
        selector.Height.ShouldBe(48f);
    }

    [Fact]
    public void LeftTopWidthHeight_WhenSetProgrammaticallyWithNoSideGrabbed_ShouldNotSnapForDisplay()
    {
        var selector = CreateSelector();

        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.Left.ShouldBe(13f);
        selector.Top.ShouldBe(7f);
        selector.Width.ShouldBe(27f);
        selector.Height.ShouldBe(41f);
    }

    [Fact]
    public void LeftTop_WhileMiddleIsGrabbed_ShouldSnapToGridForDisplayButLeaveSizeAlone()
    {
        var selector = CreateSelector();
        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.SideGrabbed = ResizeSide.Middle;

        selector.Left.ShouldBe(16f);
        selector.Top.ShouldBe(0f);
        selector.Width.ShouldBe(27f);
        selector.Height.ShouldBe(41f);
    }

    [Fact]
    public void WidthHeight_WhileResizeSideIsGrabbed_ShouldSnapToGridForDisplayButLeavePositionAlone()
    {
        var selector = CreateSelector();
        selector.Left = 13f;
        selector.Top = 7f;
        selector.Width = 27f;
        selector.Height = 41f;

        selector.SideGrabbed = ResizeSide.BottomRight;

        selector.Left.ShouldBe(13f);
        selector.Top.ShouldBe(7f);
        selector.Width.ShouldBe(32f);
        selector.Height.ShouldBe(48f);
    }
}
