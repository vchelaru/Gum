using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

// Issue #3561: SystemManagers.LoadEmbeddedTexture2d mirrors the MonoGame/Raylib method of the
// same name so Gum.Forms.FormsUtilities.InitializeDefaults can load the shared UISpriteSheet.png
// on Skia. No pre-existing coverage existed for this method on the Skia backend.
public class LoadEmbeddedTexture2dTests
{
    [Fact]
    public void LoadEmbeddedTexture2d_WhenResourceExists_ShouldReturnDecodedBitmap()
    {
        SystemManagers systemManagers = new SystemManagers();
        systemManagers.Initialize();

        SKBitmap? texture = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png");

        texture.ShouldNotBeNull();
        texture.Width.ShouldBeGreaterThan(0);
        texture.Height.ShouldBeGreaterThan(0);
    }

    // GetOrLoadEmbeddedTexture2d reuses the cached bitmap instead of decoding a fresh one per call,
    // which is what lets Styling and FormsUtilities share one line across every backend. Issue #4451.
    [Fact]
    public void GetOrLoadEmbeddedTexture2d_CalledTwice_ShouldReturnTheSameBitmap()
    {
        SystemManagers systemManagers = new SystemManagers();
        systemManagers.Initialize();

        SKBitmap first = systemManagers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
        SKBitmap second = systemManagers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
    }
}
