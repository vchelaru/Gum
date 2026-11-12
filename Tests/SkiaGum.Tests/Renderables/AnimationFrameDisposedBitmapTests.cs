using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A chain frame whose bitmap was disposed after the animation was set up (content unload) must not crash
/// the process reading its size (#7948); the frame shows nothing, like a frame with no texture.
/// </summary>
public class AnimationFrameDisposedBitmapTests
{
    private static AnimationChainList CreateChains(SKBitmap live, SKBitmap toBeDisposed)
    {
        AnimationChain first = new() { Name = "Live" };
        first.Add(new AnimationFrame { FrameLength = 1, Texture = live, RightCoordinate = 1, BottomCoordinate = 1 });
        AnimationChain second = new() { Name = "Disposed" };
        second.Add(new AnimationFrame { FrameLength = 1, Texture = toBeDisposed, RightCoordinate = 1, BottomCoordinate = 1 });
        AnimationChainList chains = new();
        chains.Add(first);
        chains.Add(second);
        return chains;
    }

    [Fact]
    public void Sprite_FrameBitmapDisposed_ShowsNothingWithoutCrashing()
    {
        using SKBitmap live = new(8, 8);
        SKBitmap disposed = new(8, 8);
        Sprite sprite = new();
        sprite.AnimationLogic.AnimationChains = CreateChains(live, disposed);
        sprite.AnimationLogic.CurrentChainName = "Live";
        sprite.SourceRectangle.ShouldNotBeNull();
        disposed.Dispose();

        sprite.AnimationLogic.CurrentChainName = "Disposed";

        sprite.Texture.ShouldBeNull();
        sprite.SourceRectangle.ShouldBeNull();
    }

    [Fact]
    public void NineSlice_FrameBitmapDisposed_ShowsNothingWithoutCrashing()
    {
        using SKBitmap live = new(8, 8);
        SKBitmap disposed = new(8, 8);
        NineSlice nineSlice = new();
        nineSlice.AnimationLogic.AnimationChains = CreateChains(live, disposed);
        nineSlice.AnimationLogic.CurrentChainName = "Live";
        nineSlice.SourceRectangle.ShouldNotBeNull();
        disposed.Dispose();

        nineSlice.AnimationLogic.CurrentChainName = "Disposed";

        nineSlice.Texture.ShouldBeNull();
        nineSlice.SourceRectangle.ShouldBeNull();
    }
}
