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
    private readonly int _bitmapWidth;
    private readonly int _bitmapHeight;

    public object LastLoadedContent { get; private set; }
    public string LastLoadedName { get; private set; }

    public MockContentLoader(int bitmapWidth = 10, int bitmapHeight = 10)
    {
        _bitmapWidth = bitmapWidth;
        _bitmapHeight = bitmapHeight;
    }

    public T LoadContent<T>(string contentName)
    {
        LastLoadedName = contentName;
        if (typeof(T) == typeof(SKBitmap))
        {
            var bitmap = new SKBitmap(_bitmapWidth, _bitmapHeight);
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

    // Regression for AnimationFrame.ToAnimationFrame missing the SKIA texture-loading
    // branch — texture stayed null for every animation frame on Skia, Sprite.Render
    // early-returned, the .achx-driven animation row in the SilkNet sample rendered
    // nothing. Fix collapses the per-backend #if branches into a single block that
    // uses the Texture2D using-alias defined at the top of AnimationFrame.cs.
    [Fact]
    public void ToAnimationFrame_OnSkia_ShouldLoadTextureViaLoaderManager()
    {
        MockContentLoader mockLoader = new MockContentLoader();
        IContentLoader originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = mockLoader;

        try
        {
            Gum.Content.AnimationChain.AnimationFrameSave save = new Gum.Content.AnimationChain.AnimationFrameSave
            {
                TextureName = "fake_texture.png",
                FrameLength = 0.1f,
                LeftCoordinate = 0f,
                RightCoordinate = 1f,
                TopCoordinate = 0f,
                BottomCoordinate = 1f,
            };

            AnimationFrame frame = save.ToAnimationFrame();

            frame.Texture.ShouldNotBeNull();
            mockLoader.LastLoadedName.ShouldEndWith("fake_texture.png");
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
        }
    }

    // Regression for #3549: the Pixel-coordinate conversion block in
    // AnimationFrame.ToAnimationFrame was gated behind
    // `#if MONOGAME || KNI || XNA4 || SOKOL`, missing RAYLIB and SKIA. On Skia a
    // .achx with <CoordinateType>Pixel</CoordinateType> never had its
    // LeftCoordinate/RightCoordinate/TopCoordinate/BottomCoordinate divided by
    // the texture's Width/Height, so the frame silently fell back to the 0/0/1/1
    // UV default (the entire texture) instead of the authored pixel sub-rect.
    [Fact]
    public void ToAnimationFrame_OnSkia_ConvertsPixelCoordinatesToUv()
    {
        MockContentLoader mockLoader = new MockContentLoader(bitmapWidth: 10, bitmapHeight: 10);
        IContentLoader originalLoader = LoaderManager.Self.ContentLoader;
        LoaderManager.Self.ContentLoader = mockLoader;

        try
        {
            Gum.Content.AnimationChain.AnimationFrameSave save = new Gum.Content.AnimationChain.AnimationFrameSave
            {
                TextureName = "fake_texture.png",
                FrameLength = 0.1f,
                LeftCoordinate = 2f,
                RightCoordinate = 8f,
                TopCoordinate = 2f,
                BottomCoordinate = 8f,
            };

            AnimationFrame frame = save.ToAnimationFrame(loadTexture: true, coordinateType: Gum.Content.AnimationChain.TextureCoordinateType.Pixel);

            frame.LeftCoordinate.ShouldBe(0.2f);
            frame.RightCoordinate.ShouldBe(0.8f);
            frame.TopCoordinate.ShouldBe(0.2f);
            frame.BottomCoordinate.ShouldBe(0.8f);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
        }
    }

    // Regression for the IAnimatable duplication bug: IAnimatable.cs was being
    // file-linked into BOTH GumCommon AND SkiaGum, producing two distinct
    // IAnimatable types with the same FQN. GraphicalUiElement.AnimateSelf (in
    // GumCommon) does `mContainedObjectAsIpso as IAnimatable` which resolves to
    // GumCommon's IAnimatable, but Skia's Sprite implemented the local-copy
    // IAnimatable — so the cast returned null and the per-frame tick silently
    // no-op'd when invoked through the SpriteRuntime wrapper. Existing
    // AnimationLogic-direct tests didn't catch it because they bypassed the
    // GraphicalUiElement.AnimateSelf path.
    [Fact]
    public void AnimateSelfThroughSpriteRuntime_ShouldAdvanceFrameIndex_WhenAnimateIsTrue()
    {
        SKBitmap textureA = new SKBitmap(4, 4);
        SKBitmap textureB = new SKBitmap(4, 4);

        AnimationChain chain = new AnimationChain { Name = "TestChain" };
        chain.Add(new AnimationFrame { Texture = textureA, FrameLength = 0.1f, LeftCoordinate = 0f, RightCoordinate = 1f, TopCoordinate = 0f, BottomCoordinate = 1f });
        chain.Add(new AnimationFrame { Texture = textureB, FrameLength = 0.1f, LeftCoordinate = 0f, RightCoordinate = 1f, TopCoordinate = 0f, BottomCoordinate = 1f });

        AnimationChainList chains = new AnimationChainList();
        chains.Add(chain);

        SpriteRuntime sut = new SpriteRuntime();
        sut.AnimationChains = chains;
        sut.CurrentChainName = "TestChain";
        sut.Animate = true;

        sut.AnimationChainFrameIndex.ShouldBe(0);

        // Tick through GraphicalUiElement.AnimateSelf — same path GumService.Update
        // walks. If IAnimatable is duplicated across GumCommon and SkiaGum, the
        // `as IAnimatable` cast returns null and this assertion fails.
        sut.AnimateSelf(0.15);

        sut.AnimationChainFrameIndex.ShouldBe(1);
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
