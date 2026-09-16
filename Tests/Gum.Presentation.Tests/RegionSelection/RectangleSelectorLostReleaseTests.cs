using InputLibrary;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using ResizeSide = FlatRedBall.SpecializedXnaControls.RegionSelection.ResizeSide;
// See RectangleSelectorDragRoundingTests for why this alias needs its own name.
using TexCoordRectangleSelector = FlatRedBall.SpecializedXnaControls.RegionSelection.RectangleSelector;

namespace Gum.Presentation.Tests.RegionSelection;

/// <summary>
/// A handle drag whose release the selector never sees - the canvas skips selector activity while
/// the camera pans (issue #4791), and MouseActivity itself skips when the cursor is off the canvas -
/// must still end when the selector next runs with the button up, instead of staying grabbed until
/// the next click raises a spurious EndRegionChanged (an extra undo entry).
/// </summary>
public class RectangleSelectorLostReleaseTests
{
    private readonly Mock<IInputHostControl> _host = new();
    private readonly Cursor _cursor = new();
    private readonly Keyboard _keyboard = new();
    private readonly TexCoordRectangleSelector _selector;
    private HostPointerState _pointer;
    private int _endRegionChangedCount;
    private double _time;

    public RectangleSelectorLostReleaseTests()
    {
        _host.SetupGet(h => h.Focused).Returns(true);
        _host.SetupGet(h => h.Width).Returns(200);
        _host.SetupGet(h => h.Height).Returns(200);
        _host.SetupProperty(h => h.Cursor, CursorKind.Arrow);
        _host.Setup(h => h.GetPointerState()).Returns(() => _pointer);
        _cursor.Initialize(_host.Object);
        _keyboard.Initialize(_host.Object);

        // Camera transform needs no graphics device - see RectangleSelectorCursorTests.
        _selector = new TexCoordRectangleSelector(new SystemManagers { Renderer = new Renderer() })
        {
            Visible = true,
            Left = 10,
            Top = 10,
            Width = 40,
            Height = 40
        };
        _selector.EndRegionChanged += (_, _) => _endRegionChangedCount++;
    }

    /// <summary>Advances the shared input one frame; the selector only sees it when the canvas lets it.</summary>
    private void Frame(float x, float y, bool isLeftDown, bool runSelector)
    {
        _pointer = new HostPointerState(x, y, isLeftDown, isRightDown: false, isMiddleDown: false);
        _cursor.Activity(_time++);
        _keyboard.Activity();
        if (runSelector)
        {
            _selector.Activity(_cursor, _keyboard, _host.Object);
        }
    }

    [Fact]
    public void Activity_ShouldEndTheDragOnce_WhenTheReleaseHappenedWhileTheCanvasWasPanning()
    {
        Frame(30, 30, isLeftDown: true, runSelector: true);
        _selector.SideGrabbed.ShouldBe(ResizeSide.Middle);
        Frame(35, 35, isLeftDown: true, runSelector: true);

        // A middle press starts a camera pan, so the canvas skips the selector - and the left
        // release lands on a frame it never sees.
        Frame(35, 35, isLeftDown: false, runSelector: false);

        // Pan over; the selector runs again with the button already up.
        Frame(35, 35, isLeftDown: false, runSelector: true);

        _selector.SideGrabbed.ShouldBe(ResizeSide.None);
        _endRegionChangedCount.ShouldBe(1);

        // A later click on empty space is an ordinary click, not the end of the old drag.
        Frame(150, 150, isLeftDown: true, runSelector: true);
        Frame(150, 150, isLeftDown: false, runSelector: true);

        _endRegionChangedCount.ShouldBe(1);
    }

    [Fact]
    public void Activity_ShouldEndTheDragOnce_WhenTheReleaseHappenedOffTheCanvas()
    {
        Frame(30, 30, isLeftDown: true, runSelector: true);
        Frame(35, 35, isLeftDown: true, runSelector: true);

        // Off-canvas frames: MouseActivity gates on IsInWindow, so the release edge is never seen.
        Frame(-5, 35, isLeftDown: true, runSelector: true);
        Frame(-5, 35, isLeftDown: false, runSelector: true);

        _selector.SideGrabbed.ShouldBe(ResizeSide.None);
        _endRegionChangedCount.ShouldBe(1);
    }
}
