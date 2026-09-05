using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;

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

    // ---- Dispatcher routing pins (issue #3639 / ADR 0011) -----------------------------------
    // These drive the STRING property name through the production Skia dispatcher (via
    // SetProperty) and assert the value lands on ContainerRuntime. Unlike Sprite/NineSlice,
    // ContainerRuntime.Alpha/IsRenderTarget don't call NotifyPropertyChanged, so redispatching
    // through the runtime here is a structural convergence with the core dispatcher (matching
    // #4034's core-file redispatch), not a bug fix -- these pins protect that routing going
    // forward rather than proving a red-to-green behavior change.

    [Fact]
    public void Dispatch_Alpha_RoutesToRuntime()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Dispatch_Alpha_WithFloatValue_TruncatesToInt()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("Alpha", 128.9f);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Dispatch_Alpha_WithUnsupportedValueType_DefaultsTo255()
    {
        ContainerRuntime sut = new();
        sut.Alpha = 10;

        sut.SetProperty("Alpha", "not a number");

        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Dispatch_IsRenderTarget_RoutesToRuntime()
    {
        ContainerRuntime sut = new();

        sut.SetProperty("IsRenderTarget", true);

        sut.IsRenderTarget.ShouldBeTrue();
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
    public void RenderTargetEffect_DefaultsToNull()
    {
        ContainerRuntime sut = new();
        sut.RenderTargetEffect.ShouldBeNull();
    }

    [Fact]
    public void RenderTargetEffect_SetThenGet_RoundTrips()
    {
        ContainerRuntime sut = new();
        SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(
            "uniform shader inputImage; half4 main(float2 coord) { return inputImage.eval(coord); }",
            out string errors);
        string.IsNullOrEmpty(errors).ShouldBeTrue(errors);

        sut.RenderTargetEffect = effect;

        sut.RenderTargetEffect.ShouldBe(effect);
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
