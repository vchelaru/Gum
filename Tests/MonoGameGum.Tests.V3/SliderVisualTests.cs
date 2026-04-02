using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V3;

public class SliderVisualTests
{
    [Fact]
    public void BackgroundColor_Property_ShouldNotExist()
    {
        PropertyInfo? property = typeof(SliderVisual).GetProperty("BackgroundColor");

        property.ShouldBeNull("BackgroundColor was removed because it was dead code with no visual element to target");
    }

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        Slider slider = new();
        InteractiveGue visual = slider.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (ContainerRuntime child in children)
        {
            if (child != slider.Track)
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

        slider.Track!.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void FocusedIndicatorColor_ShouldApplyToFocusedIndicator()
    {
        Slider slider = new();
        SliderVisual visual = (SliderVisual)slider.Visual;
        Color expected = new Color(255, 0, 0);

        visual.FocusedIndicatorColor = expected;

        visual.FocusedIndicator.Color.ShouldBe(expected);
    }

    [Fact]
    public void TrackBackgroundColor_ShouldApplyToTrackBackground()
    {
        Slider slider = new();
        SliderVisual visual = (SliderVisual)slider.Visual;
        Color expected = new Color(0, 255, 0);

        visual.TrackBackgroundColor = expected;

        visual.TrackBackground.Color.ShouldBe(expected);
    }
}
