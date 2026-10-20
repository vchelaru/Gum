using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
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
    public void IsEnabled_ShouldDisableAndRestoreUpAndDownButtons()
    {
        ScrollBar scrollBar = new();

        scrollBar.IsEnabled = false;

        scrollBar.UpButton!.IsEnabled.ShouldBeFalse();
        scrollBar.DownButton!.IsEnabled.ShouldBeFalse();

        scrollBar.IsEnabled = true;

        scrollBar.UpButton.IsEnabled.ShouldBeTrue();
        scrollBar.DownButton.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void VisualIsEnabled_ShouldDisableUpAndDownButtons_WhenSetToFalse()
    {
        ScrollBar scrollBar = new();

        scrollBar.Visual.IsEnabled = false;

        scrollBar.IsEnabled.ShouldBeFalse();
        scrollBar.UpButton!.IsEnabled.ShouldBeFalse();
        scrollBar.DownButton!.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Visual_ShouldDisableButtonsAndThumb_WhenReassignedWhileDisabled()
    {
        ScrollBar scrollBar = new();
        scrollBar.IsEnabled = false;

        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        scrollBar.Visual = newVisual;

        Button newThumb = (Button)newVisual.ThumbInstance.FormsControlAsObject!;
        scrollBar.UpButton!.Visual.ShouldBe(newVisual.UpButtonInstance);
        scrollBar.UpButton.IsEnabled.ShouldBeFalse();
        scrollBar.DownButton!.IsEnabled.ShouldBeFalse();
        newThumb.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Visual_ShouldClearButtonsAndUnhookOldOnes_WhenReassignedToVisualWithoutButtons()
    {
        ScrollBar scrollBar = new();
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.SmallChange = 2;
        Button oldDownButton = scrollBar.DownButton!;
        oldDownButton.Visual.TryCallPush();
        scrollBar.Value.ShouldBe(2);

        scrollBar.Visual = new InteractiveGue();

        scrollBar.UpButton.ShouldBeNull();
        scrollBar.DownButton.ShouldBeNull();
        scrollBar.Maximum = 100;
        double valueBeforePush = scrollBar.Value;
        oldDownButton.Visual.TryCallPush();
        scrollBar.Value.ShouldBe(valueBeforePush);
    }

    [Fact]
    public void Constructor_ShouldResolveButtons_FromAuthoredVisual()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);

        ScrollBar sut = new(visual);

        sut.UpButton!.Visual.ShouldBe(visual.UpButtonInstance);
        sut.DownButton!.Visual.ShouldBe(visual.DownButtonInstance);
    }

    [Fact]
    public void Visual_ShouldResolveButtonsFromNewVisual_WhenReassigned()
    {
        ScrollBar sut = new();
        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        sut.Visual = newVisual;

        sut.UpButton!.Visual.ShouldBe(newVisual.UpButtonInstance);
        sut.DownButton!.Visual.ShouldBe(newVisual.DownButtonInstance);
    }

    [Fact]
    public void Orientation_ShouldStayHorizontal_WhenVisualIsReassigned()
    {
        ScrollBar scrollBar = new();
        scrollBar.Orientation = Orientation.Horizontal;

        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        scrollBar.Visual = newVisual;

        scrollBar.Orientation.ShouldBe(Orientation.Horizontal);
        // The Horizontal state (not the Vertical one) must be applied to the new visual.
        newVisual.Width.ShouldBe(128f);
        newVisual.Height.ShouldBe(24f);
    }

    [Fact]
    public void Orientation_ShouldStayVertical_WhenVisualIsReassigned()
    {
        ScrollBar scrollBar = new();

        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.States.OrientationStates.Horizontal.Apply!();
        scrollBar.Visual = newVisual;

        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
        newVisual.Width.ShouldBe(24f);
        newVisual.Height.ShouldBe(128f);
    }

    [Fact]
    public void MinimumThumbSize_ShouldNotMakeVerticalThumbExceedTrack()
    {
        ScrollBarVisual visual = new();
        ScrollBar scrollBar = visual.FormsControl;
        visual.Height = 74; // two 24 px buttons leave a 26 px track
        scrollBar.MinimumThumbSize = 40;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 20;
        scrollBar.Value = 100;
        visual.UpdateLayout();

        float trackHeight = scrollBar.Track!.AbsoluteHeight;
        trackHeight.ShouldBe(26f);
        visual.ThumbInstance.AbsoluteHeight.ShouldBe(trackHeight);
        visual.ThumbInstance.Y.ShouldBe(0f, 0.01f);
    }

    [Fact]
    public void MinimumThumbSize_ShouldNotMakeHorizontalThumbExceedTrack()
    {
        ScrollBarVisual visual = new();
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Horizontal;
        visual.Width = 74; // two 24 px buttons leave a 26 px track
        scrollBar.MinimumThumbSize = 40;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 20;
        scrollBar.Value = 100;
        visual.UpdateLayout();

        float trackWidth = scrollBar.Track!.AbsoluteWidth;
        trackWidth.ShouldBe(26f);
        visual.ThumbInstance.AbsoluteWidth.ShouldBe(trackWidth);
        visual.ThumbInstance.X.ShouldBe(0f, 0.01f);
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
