using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using Gum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ScrollBarTests : BaseTestClass
{

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollBar scrollBar = new();
        InteractiveGue visual = scrollBar.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        foreach (var child in children)
        {
            if(child != scrollBar.Track)
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

        scrollBar.Track!.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollBar sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }


    [Fact]
    public void ScrollBar_Orientation_ShouldDefaultToVertical()
    {
        ScrollBar scrollBar = new();
        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
    }

    [Fact]
    public void TrackPush_HorizontalRightOfNarrowThumb_ShouldPageByLargeChange()
    {
        ScrollBar scrollBar = CreateHorizontalScrollBarWithMinimumThumb();
        GraphicalUiElement thumb = scrollBar.Visual.Descendants().First(d => d.Name == "ThumbInstance");
        thumb.AbsoluteWidth.ShouldBeLessThan(thumb.AbsoluteHeight,
            "Test needs a thumb narrower than it is tall to expose the width/height mix-up");

        // 4 px right of the thumb's right edge
        SetCursorX(thumb.AbsoluteLeft + thumb.AbsoluteWidth + 4);
        scrollBar.Track!.TryCallPush();

        scrollBar.Value.ShouldBe(10);
    }

    [Fact]
    public void TrackPush_HorizontalOnThumb_ShouldNotPage()
    {
        ScrollBar scrollBar = CreateHorizontalScrollBarWithMinimumThumb();
        GraphicalUiElement thumb = scrollBar.Visual.Descendants().First(d => d.Name == "ThumbInstance");

        SetCursorX(thumb.AbsoluteLeft + thumb.AbsoluteWidth - 1);
        scrollBar.Track!.TryCallPush();

        scrollBar.Value.ShouldBe(0);
    }

    private static ScrollBar CreateHorizontalScrollBarWithMinimumThumb()
    {
        ScrollBar scrollBar = new();
        scrollBar.AddToRoot();
        scrollBar.Orientation = Orientation.Horizontal;
        scrollBar.Visual.X = 10;
        scrollBar.Visual.Y = 10;
        scrollBar.Visual.Width = 140;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 5;
        scrollBar.LargeChange = 10;
        scrollBar.Value = 0;
        return scrollBar;
    }

    private static void SetCursorX(float x)
    {
        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        mockCursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(13);
        FrameworkElement.MainCursor = mockCursor.Object;
    }

    [Fact]
    public void Track_ShouldHaveEventsEnabled()
    {
        // Track must have HasEvents = true so it receives Push events for click-to-seek.
        // Currently ScrollBar.Track returns ThumbContainer (via a fallback path) which has
        // HasEvents = true. If Track is ever changed to point to TrackInstance directly,
        // that element must also have HasEvents = true or track clicking will silently break.
        ScrollBar scrollBar = new();

        scrollBar.Track!.HasEvents.ShouldBeTrue(
            "Track must have HasEvents = true to receive Push events for click-to-seek behavior");
    }
}
