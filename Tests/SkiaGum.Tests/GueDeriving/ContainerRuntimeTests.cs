using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;

namespace SkiaGum.Tests.GueDeriving;

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
    public void Blend_DefaultsToNormal()
    {
        ContainerRuntime sut = new();
        sut.Blend.ShouldBe(Blend.Normal);
    }

    [Fact]
    public void Blend_SetToAdditive_RoundTripsThroughBlendState()
    {
        ContainerRuntime sut = new();

        sut.Blend = Blend.Additive;

        sut.Blend.ShouldBe(Blend.Additive);
        sut.BlendState.ShouldBe(Gum.BlendState.Additive);
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
