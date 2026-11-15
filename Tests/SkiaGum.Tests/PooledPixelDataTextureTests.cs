using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests;

/// <summary>
/// <see cref="GraphicalUiElement.ApplyPooledTextureFromPixelData"/> on Skia: the process-wide pool
/// hands a detached owner's bitmap to the next caller, which must only happen when the bitmap has
/// the size the caller's pixel data needs. Issue #7885.
/// </summary>
public class PooledPixelDataTextureTests
{
    public PooledPixelDataTextureTests()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfLargerSize_GivesTheNextOwnerABitmapOfItsOwnSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime largeOwner = Attach(root);
        SpriteRuntime largeTarget = new SpriteRuntime();
        ApplyPooled(largeTarget, largeOwner, size: 9, red: 200);
        largeOwner.RemoveFromRoot();

        ContainerRuntime smallOwner = Attach(root);
        SpriteRuntime smallTarget = new SpriteRuntime();
        SKBitmap smallBitmap = ApplyPooled(smallTarget, smallOwner, size: 5, red: 40);

        smallBitmap.Width.ShouldBe(5);
        smallBitmap.Height.ShouldBe(5);
        smallBitmap.GetPixel(0, 0).ShouldBe(new SKColor(40, 0, 0, 255));
        smallBitmap.GetPixel(4, 4).ShouldBe(new SKColor(40, 0, 0, 255));
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfSmallerSize_GivesTheNextOwnerABitmapOfItsOwnSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime smallOwner = Attach(root);
        SpriteRuntime smallTarget = new SpriteRuntime();
        SKBitmap smallBitmap = ApplyPooled(smallTarget, smallOwner, size: 6, red: 40);
        smallOwner.RemoveFromRoot();

        ContainerRuntime largeOwner = Attach(root);
        SpriteRuntime largeTarget = new SpriteRuntime();
        SKBitmap largeBitmap = ApplyPooled(largeTarget, largeOwner, size: 11, red: 200);

        largeBitmap.Width.ShouldBe(11);
        largeBitmap.Height.ShouldBe(11);
        largeBitmap.GetPixel(10, 10).ShouldBe(new SKColor(200, 0, 0, 255));
        // The detached owner's bitmap was not resized or overwritten for the new owner.
        smallBitmap.Width.ShouldBe(6);
        smallBitmap.GetPixel(5, 5).ShouldBe(new SKColor(40, 0, 0, 255));
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfSameSize_ReusesTheBitmap()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime firstOwner = Attach(root);
        SKBitmap first = ApplyPooled(new SpriteRuntime(), firstOwner, size: 7, red: 40);
        firstOwner.RemoveFromRoot();

        SKBitmap second = ApplyPooled(new SpriteRuntime(), Attach(root), size: 7, red: 90);

        second.ShouldBeSameAs(first);
        second.GetPixel(3, 3).ShouldBe(new SKColor(90, 0, 0, 255));
    }

    // The size rule is tested with plain numbers: a test that handed oversized data to a real bitmap
    // would write past its native buffer if the rule ever regressed, aborting the test host.
    [Fact]
    public void EnsureFits_DataLargerThanTheBitmap_Throws()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => RenderingLibrary.PixelDataTextureApplier.EnsureFits(bitmapByteCount: 36, dataLength: 100));

        exception.Message.ShouldContain("100 bytes");
        exception.Message.ShouldContain("36");
    }

    [Fact]
    public void EnsureFits_DataOneByteOverTheBitmap_Throws()
    {
        Should.Throw<ArgumentException>(
            () => RenderingLibrary.PixelDataTextureApplier.EnsureFits(bitmapByteCount: 36, dataLength: 37));
    }

    [Theory]
    [InlineData(36, 36)]
    [InlineData(36, 8)]
    [InlineData(36, 0)]
    public void EnsureFits_DataAtMostTheBitmapSize_DoesNotThrow(long bitmapByteCount, int dataLength)
    {
        Should.NotThrow(
            () => RenderingLibrary.PixelDataTextureApplier.EnsureFits(bitmapByteCount, dataLength));
    }

    [Fact]
    public void Upload_DataSmallerThanTheBitmap_CopiesItWithoutTouchingTheRest()
    {
        using SKBitmap bitmap = new SKBitmap(new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(new SKColor(0, 0, 0, 0));
        byte[] firstPixelOnly = { 11, 0, 0, 255 };

        RenderingLibrary.PixelDataTextureApplier.Upload(bitmap, firstPixelOnly);

        bitmap.GetPixel(0, 0).ShouldBe(new SKColor(11, 0, 0, 255));
        bitmap.GetPixel(1, 1).ShouldBe(new SKColor(0, 0, 0, 0));
    }

    [Fact]
    public void ApplyPooled_DataShorterThanTheSize_ThrowsAndLeavesTheSpriteAlone()
    {
        ContainerRuntime root = new ContainerRuntime();
        SpriteRuntime target = new SpriteRuntime();

        Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyPooledTextureFromPixelData!(target, Attach(root), new byte[8], 6, 6));

        ((Sprite)target.RenderableComponent).Texture.ShouldBeNull();
    }

    [Fact]
    public void ApplyPooled_DataLongerThanTheSize_Throws()
    {
        ContainerRuntime root = new ContainerRuntime();

        Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyPooledTextureFromPixelData!(
                new SpriteRuntime(), Attach(root), new byte[6 * 6 * 4 + 4], 6, 6));
    }

    [Fact]
    public void ApplyCached_DataShorterThanTheSize_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyCachedTextureFromPixelData!(
                new SpriteRuntime(), "PooledPixelDataTextureTests.ShortData", new byte[8], 6, 6));
    }

    [Fact]
    public void ApplyPooled_BadDataWhenAnOwnerChangesSize_KeepsTheOwnersBitmapFromOtherOwners()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime ownerA = Attach(root);
        SpriteRuntime targetA = new SpriteRuntime();
        SKBitmap bitmapA = ApplyPooled(targetA, ownerA, size: 6, red: 40);

        Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyPooledTextureFromPixelData!(targetA, ownerA, new byte[8], 9, 9));
        SKBitmap bitmapB = ApplyPooled(new SpriteRuntime(), Attach(root), size: 6, red: 200);

        bitmapB.ShouldNotBeSameAs(bitmapA);
        ((Sprite)targetA.RenderableComponent).Texture.ShouldBeSameAs(bitmapA);
        bitmapA.GetPixel(0, 0).ShouldBe(new SKColor(40, 0, 0, 255));
    }

    [Fact]
    public void Upload_DataExactlyTheBitmapSize_CopiesEveryPixel()
    {
        using SKBitmap bitmap = new SKBitmap(new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul));
        byte[] exact = new byte[2 * 2 * 4];
        exact[^4] = 77;
        exact[^1] = 255;

        RenderingLibrary.PixelDataTextureApplier.Upload(bitmap, exact);

        bitmap.GetPixel(1, 1).ShouldBe(new SKColor(77, 0, 0, 255));
    }

    private static ContainerRuntime Attach(ContainerRuntime root)
    {
        ContainerRuntime owner = new ContainerRuntime();
        root.AddChild(owner);
        return owner;
    }

    private static SKBitmap ApplyPooled(SpriteRuntime target, GraphicalUiElement owner, int size, byte red)
    {
        byte[] rgba = new byte[size * size * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = red;
            rgba[i + 3] = 255;
        }

        GraphicalUiElement.ApplyPooledTextureFromPixelData!(target, owner, rgba, size, size);

        return ((Sprite)target.RenderableComponent).Texture!;
    }
}
