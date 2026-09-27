using System;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

public class SkiaObjectTextureRendererTests
{
    [Fact]
    public void SnapshotToTexture_ShouldDisposeTheSnapshot()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(2, 2));
        SKImage? snapshot = null;

        SkiaObjectTextureRenderer.SnapshotToTexture(surface, image =>
        {
            snapshot = image;
            image.Handle.ShouldNotBe(IntPtr.Zero);
            return null!;
        });

        snapshot.ShouldNotBeNull();
        snapshot.Handle.ShouldBe(IntPtr.Zero);
    }
}
