using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class ContainerRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        ContainerRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        ContainerRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
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
    public void IsRenderTarget_ShouldDefaultToFalse()
    {
        ContainerRuntime sut = new();
        sut.IsRenderTarget.ShouldBeFalse();
    }

    [Fact]
    public void IsRenderTarget_ShouldRoundTrip()
    {
        ContainerRuntime sut = new();
        sut.IsRenderTarget = true;
        sut.IsRenderTarget.ShouldBeTrue();
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
