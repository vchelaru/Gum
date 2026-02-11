using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class ScrollBarTests
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
    public void Constructor_ShouldCreateV2Visual()
    {
        ScrollBar scrollBar = new ();
        scrollBar.Visual.ShouldNotBeNull();
        (scrollBar.Visual is ScrollBarVisual).ShouldBeTrue();
    }

    [Fact]
    public void ThumbContainer_HasEvents_ShouldBeTrue()
    {
        ScrollBar scrollBar = new();
        InteractiveGue thumbContainer = (InteractiveGue)scrollBar.Visual.Children.First(c => c.Name == "ThumbContainer");
        thumbContainer.HasEvents.ShouldBeTrue("Because ThumbContainer is used to click on for moving the value");

    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollBar sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ScrollBar_ThumbWidth_ShouldMatchScrollBarWidth()
    {
        ScrollBar scrollBar = new();
        ScrollBarVisual scrollBarVisual = (ScrollBarVisual)scrollBar.Visual;

        scrollBarVisual.ThumbInstance.GetAbsoluteWidth().ShouldBe(
            scrollBarVisual.GetAbsoluteWidth());
    }

}
