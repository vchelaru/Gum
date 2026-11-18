using Gum.GueDeriving;
using Gum.Renderables;
using Gum.RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class BlendTests : BaseTestClass
{
    [Fact]
    public void NineSliceRuntime_Blend_DefaultsToNormal()
    {
        NineSliceRuntime sut = new();
        sut.Blend.ShouldBe(Blend.Normal);
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
    public void SpriteRuntime_Blend_DefaultsToNormal()
    {
        SpriteRuntime sut = new();
        sut.Blend.ShouldBe(Blend.Normal);
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
    public void TextRuntime_Blend_DefaultsToNormal()
    {
        TextRuntime sut = new();
        sut.Blend.ShouldBe(Blend.Normal);
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

    // ContainerRuntime's Blend getter reads its BlendState back through ToBlend, so Normal must
    // survive a custom Renderer.NormalBlendState instead of reading back null.
    [Theory]
    [InlineData(Blend.Normal)]
    [InlineData(Blend.Additive)]
    [InlineData(Blend.Replace)]
    [InlineData(Blend.SubtractAlpha)]
    [InlineData(Blend.ReplaceAlpha)]
    [InlineData(Blend.MinAlpha)]
    public void ContainerRuntime_Blend_RoundTrips_WhenNormalBlendStateIsCustom(Blend blend)
    {
        Gum.BlendState previous = global::RenderingLibrary.Graphics.Renderer.NormalBlendState;
        try
        {
            global::RenderingLibrary.Graphics.Renderer.NormalBlendState = new Gum.BlendState
            {
                ColorSourceBlend = Gum.Blend.SourceAlpha,
                ColorDestinationBlend = Gum.Blend.InverseSourceAlpha,
                AlphaSourceBlend = Gum.Blend.One,
                AlphaDestinationBlend = Gum.Blend.InverseSourceAlpha,
            };
            ContainerRuntime sut = new();

            sut.Blend = blend;

            sut.Blend.ShouldBe(blend);
        }
        finally
        {
            global::RenderingLibrary.Graphics.Renderer.NormalBlendState = previous;
        }
    }
}
