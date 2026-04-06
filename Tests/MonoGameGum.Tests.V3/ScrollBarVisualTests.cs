using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ScrollBarVisualTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollBar scrollBar = new();
        InteractiveGue visual = scrollBar.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            if (child != scrollBar.Track)
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

        scrollBar.Track!.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindTrack()
    {
        // Track is found by RangeBase.RefreshInternalVisualReferences
        ScrollBar scrollBar = new();

        scrollBar.Track.ShouldNotBeNull();
    }

    [Fact]
    public void ScrollBar_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ScrollBar sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
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

    [Fact]
    public void Value_ShouldBeSettableAfterConstruction()
    {
        // Value assignment depends on both RangeBase and ScrollBar
        // RefreshInternalVisualReferences having run
        ScrollBar scrollBar = new();

        scrollBar.Value = 50;

        scrollBar.Value.ShouldBe(50);
    }
}
