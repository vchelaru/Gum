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
}
