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

public class ScrollViewerTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollViewer scrollViewer = new();
        InteractiveGue visual = scrollViewer.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            if (child.Name != "ThumbContainer")
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }
    }

    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var scrollViewer = new Gum.Forms.Controls.ScrollViewer();
        scrollViewer.Visual.ShouldNotBeNull();
        (scrollViewer.Visual is Gum.Forms.DefaultVisuals.ScrollViewerVisual).ShouldBeTrue();
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollViewer sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ScrollViewerVisual_ShouldCreateScrollViewerForms()
    {
        var visual = new Gum.Forms.DefaultVisuals.ScrollViewerVisual();
        visual.FormsControl.ShouldNotBeNull();
    }
}
