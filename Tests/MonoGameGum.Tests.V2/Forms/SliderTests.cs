using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class SliderTests
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

    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var slider = new Gum.Forms.Controls.Slider();
        slider.Visual.ShouldNotBeNull();
        (slider.Visual is Gum.Forms.DefaultVisuals.SliderVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Slider sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
