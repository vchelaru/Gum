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
    public void ScrollBar_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ScrollBar sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
