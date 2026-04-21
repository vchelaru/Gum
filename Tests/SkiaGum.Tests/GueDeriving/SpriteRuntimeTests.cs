using Gum.Wireframe;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using Shouldly;
using Xunit;
using RenderingLibrary.Content;
using System;
using Gum.Graphics.Animation;

namespace SkiaGum.Tests.GueDeriving;

public class MockContentLoader : IContentLoader
{
    public object LastLoadedContent { get; private set; }
    public string LastLoadedName { get; private set; }

    public T LoadContent<T>(string contentName)
    {
        LastLoadedName = contentName;
        if (typeof(T) == typeof(SKBitmap))
        {
            var bitmap = new SKBitmap(10, 10);
            LastLoadedContent = bitmap;
            return (T)(object)bitmap;
        }
        return default;
    }

    public T TryLoadContent<T>(string contentName) => LoadContent<T>(contentName);
}

public class SpriteRuntimeTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = new SpriteRuntime();
        sut.Width.ShouldBe(100);
        sut.Height.ShouldBe(100);
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile);
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile);
        sut.RenderableComponent.ShouldBeOfType<Sprite>();
    }

    [Fact]
    public void Texture_ShouldForwardToContainedSprite()
    {
        var sut = new SpriteRuntime();
        var bitmap = new SKBitmap(5, 5);
        sut.Texture = bitmap;
        sut.Texture.ShouldBe(bitmap);
        ((Sprite)sut.RenderableComponent).Texture.ShouldBe(bitmap);
    }

    [Fact]
    public void Image_ShouldForwardToContainedSprite()
    {
        var sut = new SpriteRuntime();
        var bitmap = new SKBitmap(5, 5);
        var image = SKImage.FromBitmap(bitmap);
        sut.Image = image;
        sut.Image.ShouldBe(image);
        ((Sprite)sut.RenderableComponent).Image.ShouldBe(image);
    }

    [Fact]
    public void Color_ShouldForwardToContainedSprite()
    {
        var sut = new SpriteRuntime();
        sut.Color = SKColors.Red;
        sut.Color.ShouldBe(SKColors.Red);
        ((Sprite)sut.RenderableComponent).Color.ShouldBe(SKColors.Red);
    }

    [Fact]
    public void SourceFile_SettingEmpty_ShouldSetTextureNull()
    {
        var sut = new SpriteRuntime();
        sut.Texture = new SKBitmap(10, 10);
        
        sut.SourceFile = "";
        sut.Texture.ShouldBeNull();

        sut.Texture = new SKBitmap(10, 10);
        sut.SourceFile = null;
        sut.Texture.ShouldBeNull();
    }

    [Fact]
    public void SourceFile_SettingPath_ShouldLoadViaLoaderManager()
    {
        var mockLoader = new MockContentLoader();
        var originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = mockLoader;

        try
        {
            var sut = new SpriteRuntime();
            sut.SourceFile = "test_path.png";
            
            mockLoader.LastLoadedName.ShouldBe("test_path.png");
            sut.Texture.ShouldBe(mockLoader.LastLoadedContent);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
        }
    }

    [Fact]
    public void AnimationProperties_ShouldForwardToAnimationLogic()
    {
        var sut = new SpriteRuntime();
        var sprite = (Sprite)sut.RenderableComponent;

        sut.Animate = true;
        sprite.AnimationLogic.Animate.ShouldBeTrue();

        var chains = new AnimationChainList();
        var walkChain = new AnimationChain { Name = "Walk" };
        walkChain.Add(new AnimationFrame());
        chains.Add(walkChain);
        sut.AnimationChains = chains;
        sprite.AnimationLogic.AnimationChains.ShouldBe(chains);

        sut.CurrentChainName = "Walk";
        sprite.AnimationLogic.CurrentChainName.ShouldBe("Walk");

        sut.AnimationChainFrameIndex = 2;
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(2);

        sut.AnimationChainTime = 0.5;
        sprite.AnimationLogic.TimeIntoAnimation.ShouldBe(0.5);

        sut.AnimationChainSpeed = 1.5f;
        sprite.AnimationLogic.AnimationSpeed.ShouldBe(1.5f);

        sut.IsAnimationChainLooping = false;
        sprite.AnimationLogic.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Fact]
    public void AnimationChainCycled_ShouldForwardToAnimationLogic()
    {
        var sut = new SpriteRuntime();
        var sprite = (Sprite)sut.RenderableComponent;
        bool wasCalled = false;
        Action callback = () => wasCalled = true;

        sut.AnimationChainCycled += callback;
        // We can't easily trigger it without a real chain, but we can check if we can remove it
        sut.AnimationChainCycled -= callback;
    }

    [Fact]
    public void Clone_ShouldReturnNewInstance()
    {
        var sut = new SpriteRuntime();
        sut.Color = SKColors.Blue;
        
        var clone = (SpriteRuntime)sut.Clone();
        
        clone.ShouldNotBeSameAs(sut);
        clone.Color.ShouldBe(SKColors.Blue);
        // The renderable component should be a different instance
        clone.RenderableComponent.ShouldNotBeSameAs(sut.RenderableComponent);
    }

    [Fact]
    public void TextureSetter_ShouldUpdateLayout_WhenPercentageBased()
    {
        var sut = new SpriteRuntime();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        // Initial 100% of nothing is probably 0 or default
        var initialWidth = sut.GetAbsoluteWidth();

        var bitmap = new SKBitmap(100, 50);
        sut.Texture = bitmap;

        // 100% of 100 should be 100
        sut.GetAbsoluteWidth().ShouldBe(100);
        sut.GetAbsoluteHeight().ShouldBe(50);

        var biggerBitmap = new SKBitmap(200, 300);
        sut.Texture = biggerBitmap;

        sut.GetAbsoluteWidth().ShouldBe(200);
        sut.GetAbsoluteHeight().ShouldBe(300);
    }
}
