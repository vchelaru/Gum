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

namespace MonoGameGum.Tests.Forms;
public class SliderTests : BaseTestClass
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
    public void Visual_HasEvents_ShouldBeTrue()
    {
        Slider sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Value_ShouldBeLimited_WhenOutsideOfMinimumAndMaximum()
    {
        Slider slider = new ()
        {
            Minimum = 0,
            Maximum = 100
        };
        slider.Value = -10;
        slider.Value.ShouldBe(0);
        slider.Value = 110;
        slider.Value.ShouldBe(100);
    }

    [Fact]
    public void Value_ShouldBeAdjusted_WhenMinimumAndMaximumChange()
    {
        Slider slider = new ()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        slider.Value = 10;
        slider.Minimum = 25;
        slider.Value.ShouldBe(25);

        slider.Value = 90;
        slider.Maximum = 75;
        slider.Maximum.ShouldBe(75);
    }

    [Fact]
    public void IsFocused_ShouldUpdateState()
    {
        Slider slider = new();

        var visual = slider.Visual;

        var focusedState = visual.Categories[Slider.SliderCategoryName]
            .States.First(item => item.Name == FrameworkElement.FocusedStateName);

        focusedState.Clear();
        bool wasSet = false;
        focusedState.Apply = () =>
        {
            wasSet = true;
        };

        slider.IsFocused = true;

        wasSet.ShouldBeTrue();
    }
}
