using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace SokolGum.Tests.Runtimes;

public class ContainerRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeInvisibleRenderable()
    {
        var sut = new ContainerRuntime();
        sut.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue_ByDefault()
    {
        var sut = new ContainerRuntime();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldBeTrue_ByDefault()
    {
        var sut = new ContainerRuntime();
        sut.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void IsRenderTarget_ShouldDefaultToFalse()
    {
        var sut = new ContainerRuntime();
        sut.IsRenderTarget.ShouldBeFalse();
    }

    [Fact]
    public void Width_ShouldRoundTrip()
    {
        var sut = new ContainerRuntime { Width = 128 };
        sut.Width.ShouldBe(128);
    }

    [Fact]
    public void Height_ShouldRoundTrip()
    {
        var sut = new ContainerRuntime { Height = 64 };
        sut.Height.ShouldBe(64);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        var sut = new ContainerRuntime();
        sut.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Rotation_ShouldBeZero_ByDefault()
    {
        var sut = new ContainerRuntime();
        sut.Rotation.ShouldBe(0);
    }

    [Fact]
    public void Rotation_ShouldRoundTrip()
    {
        var sut = new ContainerRuntime { Rotation = 45f };
        sut.Rotation.ShouldBe(45f);
    }
}
