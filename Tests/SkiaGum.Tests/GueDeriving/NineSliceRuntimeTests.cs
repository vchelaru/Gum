using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class NineSliceRuntimeTests
{
    [Fact]
    public void BorderScale_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.BorderScale = 2.5f;
        sut.BorderScale.ShouldBe(2.5f);
        ((NineSlice)sut.RenderableComponent).BorderScale.ShouldBe(2.5f);
    }

    [Fact]
    public void Color_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.Color = SKColors.Lime;
        sut.Color.ShouldBe(SKColors.Lime);
        ((NineSlice)sut.RenderableComponent).Color.ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.Width.ShouldBe(100);
        sut.Height.ShouldBe(100);
        sut.RenderableComponent.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void CustomFrameTextureCoordinateWidth_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.CustomFrameTextureCoordinateWidth = 12f;
        sut.CustomFrameTextureCoordinateWidth.ShouldBe(12f);
        ((NineSlice)sut.RenderableComponent).CustomFrameTextureCoordinateWidth.ShouldBe(12f);
    }

    [Fact]
    public void IsTilingMiddleSections_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.IsTilingMiddleSections = true;
        sut.IsTilingMiddleSections.ShouldBeTrue();
        ((NineSlice)sut.RenderableComponent).IsTilingMiddleSections.ShouldBeTrue();
    }

    [Fact]
    public void Texture_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        SKBitmap bitmap = new SKBitmap(30, 30);
        sut.Texture = bitmap;
        sut.Texture.ShouldBe(bitmap);
        ((NineSlice)sut.RenderableComponent).Texture.ShouldBe(bitmap);
    }
}

public class NineSliceRenderableTests
{
    [Fact]
    public void Color_ShouldRoundTrip()
    {
        NineSlice sut = new NineSlice();
        sut.Color = SKColors.Blue;
        sut.Color.ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Defaults_ShouldMatchExpected()
    {
        NineSlice sut = new NineSlice();
        sut.IsTilingMiddleSections.ShouldBeFalse();
        sut.BorderScale.ShouldBe(1f);
        sut.CustomFrameTextureCoordinateWidth.ShouldBeNull();
        sut.Texture.ShouldBeNull();
        sut.Image.ShouldBeNull();
        // White (identity under SKBlendMode.Modulate) so a freshly constructed
        // NineSlice draws untinted instead of inheriting RenderableShapeBase's red.
        sut.Color.ShouldBe(SKColors.White);
    }

    [Fact]
    public void Texture_SettingBitmap_ShouldPopulateImage()
    {
        NineSlice sut = new NineSlice();
        SKBitmap bitmap = new SKBitmap(16, 16);
        sut.Texture = bitmap;
        sut.Image.ShouldNotBeNull();
    }

    [Fact]
    public void Texture_SettingNull_ShouldClearImage()
    {
        NineSlice sut = new NineSlice();
        sut.Texture = new SKBitmap(8, 8);
        sut.Texture = null;
        sut.Image.ShouldBeNull();
    }
}
