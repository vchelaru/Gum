using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Wireframe;

/// <summary>
/// Pins the symmetry of <see cref="GraphicalUiElement.AddToManagers(ISystemManagers, Layer?)"/> and
/// <see cref="GraphicalUiElement.RemoveFromManagers"/> on the Raylib backend (issue #3048). On Raylib
/// the contained renderable is added directly to a <see cref="Layer"/>; removal must detach it again,
/// otherwise removed visuals keep rendering every frame.
/// </summary>
public class RemoveFromManagersTests : BaseTestClass
{
    [Fact]
    public void AddToManagers_PutsRenderableOnLayer()
    {
        RectangleRuntime sut = new() { Width = 50, Height = 50 };

        sut.AddToManagers(SystemManagers.Default);

        IsContainedRenderableOnAnyLayer(sut).ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromManagers_DetachesRenderableFromLayer()
    {
        RectangleRuntime sut = new() { Width = 50, Height = 50 };
        sut.AddToManagers(SystemManagers.Default);

        sut.RemoveFromManagers();

        IsContainedRenderableOnAnyLayer(sut).ShouldBeFalse();
    }

    private static bool IsContainedRenderableOnAnyLayer(GraphicalUiElement element)
    {
        IRenderableIpso renderable = (IRenderableIpso)element.RenderableComponent;
        foreach (Layer layer in SystemManagers.Default.Renderer.Layers)
        {
            if (layer.Renderables.Contains(renderable))
            {
                return true;
            }
        }
        return false;
    }
}
