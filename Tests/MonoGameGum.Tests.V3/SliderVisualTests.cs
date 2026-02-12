using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V3;

public class SliderVisualTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        Slider slider = new();
        InteractiveGue visual = slider.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            if (child != slider.Track)
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }

        slider.Track!.HasEvents.ShouldBeTrue();
    }
}
