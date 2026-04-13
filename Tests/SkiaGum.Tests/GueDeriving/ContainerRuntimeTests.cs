using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;

namespace SkiaGum.Tests.GueDeriving;

// Note: Alpha/IsRenderTarget/BlendState round-trip tests are intentionally omitted here.
// Those properties don't exist on the Skia ContainerRuntime yet; they'll be added as part
// of the ContainerRuntime unification step that converges Skia to match MG+Raylib.
public class ContainerRuntimeTests
{
    public ContainerRuntimeTests()
    {
        // Wire up the SkiaGum custom property setter so SetProperty routes correctly.
        // Normally done by SystemManagers.Initialize(), but we don't need the full
        // rendering pipeline for these unit tests.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void ContainedRenderable_ShouldBeInvisibleRenderable()
    {
        ContainerRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Height_ShouldBe150_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Height.ShouldBe(150);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Width_ShouldBe150_ByDefault()
    {
        ContainerRuntime sut = new();
        sut.Width.ShouldBe(150);
    }
}
