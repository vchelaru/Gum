using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ScrollViewerVisualTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollViewer scrollViewer = new();
        InteractiveGue visual = scrollViewer.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        // ThumbContainer is used by ScrollBar for clicking to change value
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
    public void ScrollViewer_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ScrollViewer sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ThumbContainer_HasEvents_ShouldBeTrue()
    {
        ScrollBar scrollBar = new();
        InteractiveGue thumbContainer = (InteractiveGue)scrollBar.Visual.Children.First(c => c.Name == "ThumbContainer");
        thumbContainer.HasEvents.ShouldBeTrue("Because ThumbContainer is what is used for clicking to move the value");

    }
}
