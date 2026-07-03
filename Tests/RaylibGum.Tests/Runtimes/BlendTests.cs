using Gum.GueDeriving;
using Gum.Renderables;
using Gum.RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class BlendTests : BaseTestClass
{
    [Fact]
    public void NineSliceRuntime_Blend_DefaultsToNull()
    {
        NineSliceRuntime sut = new();
        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void NineSliceRuntime_Blend_RoundTripsThroughContainedRenderable()
    {
        NineSliceRuntime sut = new();
        sut.Blend = Blend.Additive;

        sut.Blend.ShouldBe(Blend.Additive);
        ((NineSlice)sut.RenderableComponent).Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void NineSliceRuntime_Blend_SetToNullClearsContainedRenderable()
    {
        NineSliceRuntime sut = new();
        sut.Blend = Blend.Additive;
        sut.Blend = null;

        sut.Blend.ShouldBeNull();
        ((NineSlice)sut.RenderableComponent).Blend.ShouldBeNull();
    }

    [Fact]
    public void SpriteRuntime_Blend_DefaultsToNull()
    {
        SpriteRuntime sut = new();
        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void SpriteRuntime_Blend_RoundTripsThroughContainedRenderable()
    {
        SpriteRuntime sut = new();
        sut.Blend = Blend.Additive;

        sut.Blend.ShouldBe(Blend.Additive);
        ((Sprite)sut.RenderableComponent).Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void SpriteRuntime_Blend_SetToNullClearsContainedRenderable()
    {
        SpriteRuntime sut = new();
        sut.Blend = Blend.Additive;
        sut.Blend = null;

        sut.Blend.ShouldBeNull();
        ((Sprite)sut.RenderableComponent).Blend.ShouldBeNull();
    }

    [Fact]
    public void TextRuntime_Blend_DefaultsToNull()
    {
        TextRuntime sut = new();
        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void TextRuntime_Blend_RoundTripsThroughContainedRenderable()
    {
        TextRuntime sut = new();
        sut.Blend = Blend.Additive;

        sut.Blend.ShouldBe(Blend.Additive);
        ((Text)sut.RenderableComponent).Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void TextRuntime_Blend_SetToNullClearsContainedRenderable()
    {
        TextRuntime sut = new();
        sut.Blend = Blend.Additive;
        sut.Blend = null;

        sut.Blend.ShouldBeNull();
        ((Text)sut.RenderableComponent).Blend.ShouldBeNull();
    }
}
