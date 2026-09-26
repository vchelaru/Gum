using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Zoom to fit (#5142): the unattended <c>--zoom-to-fit</c> run frames the selected element, so
/// content anchored away from the canvas's top-left is inside the screenshot.
/// </summary>
public class CanvasZoomToFitTests
{
    private static readonly int[] ZoomPercents = { 400, 200, 100, 75, 50, 33, 25, 10 };

    [Fact]
    public void Calculate_ElementLargerThanTheView_PicksTheLargestZoomThatFits_AndCentersIt()
    {
        // 2000x1000 world units into an 1000x600 view: 920x520 inside the margins, so 50% (1000 wide)
        // is too big and 33% (660x330) is the largest that fits.
        CanvasWorldBounds bounds = new CanvasWorldBounds(Left: 100, Top: 200, Right: 2100, Bottom: 1200);

        ZoomToFitResult result = CanvasZoomToFit.Calculate(bounds, viewportWidth: 1000, viewportHeight: 600, ZoomPercents);

        result.ZoomPercent.ShouldBe(33);
        float zoom = 0.33f;
        // The bounds' center (1100, 700) lands on the view's center (500, 300).
        ((1100 - result.CameraX) * zoom).ShouldBe(500, tolerance: 0.5);
        ((700 - result.CameraY) * zoom).ShouldBe(300, tolerance: 0.5);
    }

    [Fact]
    public void Calculate_SmallElement_StaysAt100Percent_AndIsCentered()
    {
        CanvasWorldBounds bounds = new CanvasWorldBounds(Left: 3000, Top: 2000, Right: 3100, Bottom: 2050);

        ZoomToFitResult result = CanvasZoomToFit.Calculate(bounds, viewportWidth: 1000, viewportHeight: 600, ZoomPercents);

        result.ZoomPercent.ShouldBe(100, "zooming in past 100% would blur the pixels a screenshot compares");
        (3050 - result.CameraX).ShouldBe(500, tolerance: 0.5);
        (2025 - result.CameraY).ShouldBe(300, tolerance: 0.5);
    }

    [Fact]
    public void Calculate_NothingFits_UsesTheSmallestZoom()
    {
        CanvasWorldBounds bounds = new CanvasWorldBounds(Left: 0, Top: 0, Right: 100_000, Bottom: 100);

        CanvasZoomToFit.Calculate(bounds, viewportWidth: 1000, viewportHeight: 600, ZoomPercents).ZoomPercent.ShouldBe(10);
    }

    [Fact]
    public void GetVisibleBounds_Component_IncludesTheRootAndVisibleDescendants_ButNotHiddenOnes()
    {
        GraphicalUiElement root = new GraphicalUiElement(new InvisibleRenderable()) { X = 0, Y = 0, Width = 100, Height = 50 };
        GraphicalUiElement child = new GraphicalUiElement(new InvisibleRenderable()) { X = 500, Y = 400, Width = 20, Height = 30 };
        GraphicalUiElement hidden = new GraphicalUiElement(new InvisibleRenderable()) { X = 9000, Y = 9000, Width = 10, Height = 10, Visible = false };
        child.Parent = root;
        hidden.Parent = root;
        root.UpdateLayout();

        CanvasZoomToFit.TryGetVisibleBounds(root, isScreen: false, out CanvasWorldBounds bounds).ShouldBeTrue();

        bounds.ShouldBe(new CanvasWorldBounds(Left: 0, Top: 0, Right: 520, Bottom: 430));
    }

    [Fact]
    public void GetVisibleBounds_Screen_UsesItsInstances_NotTheScreenItself()
    {
        GraphicalUiElement screen = new GraphicalUiElement(new InvisibleRenderable()) { Width = 800, Height = 600 };
        GraphicalUiElement farInstance = new GraphicalUiElement(new InvisibleRenderable()) { X = 2400, Y = 1600, Width = 200, Height = 100 };
        farInstance.ElementGueContainingThis = screen;
        farInstance.UpdateLayout();

        CanvasZoomToFit.TryGetVisibleBounds(screen, isScreen: true, out CanvasWorldBounds bounds).ShouldBeTrue();

        bounds.ShouldBe(new CanvasWorldBounds(Left: 2400, Top: 1600, Right: 2600, Bottom: 1700));
    }

    [Fact]
    public void GetVisibleBounds_ScreenWithNoVisibleInstances_HasNothingToFit()
    {
        GraphicalUiElement screen = new GraphicalUiElement(new InvisibleRenderable()) { Width = 800, Height = 600 };

        CanvasZoomToFit.TryGetVisibleBounds(screen, isScreen: true, out _).ShouldBeFalse();
    }
}
