using Gum.Wireframe;
using Xunit;

namespace AvaloniaCanvasSpike.Tests;

/// <summary>
/// Creates the backend device with no host window, renders the sample project into the pixel
/// buffer, and hit-tests it. Needs a GL-capable machine or the Mesa override CI uses.
/// </summary>
public class HeadlessRenderTests
{
    [Fact]
    public void RenderFrame_FillsPixelBuffer_AndHitTestFindsElement()
    {
        SpikeOptions options = SpikeOptions.Parse("test", Array.Empty<string>());

        using GumCanvasGame renderer = new GumCanvasGame(options.ProjectPath, options.ElementName);
        renderer.Resize(320, 240);
        renderer.RenderFrame();
        renderer.RenderFrame();

        byte[] pixels = renderer.PixelBuffer;
        Assert.Equal(320 * 240 * 4, pixels.Length);

        // The clear color is (40, 40, 40); something must have drawn over it.
        bool anyDrawnPixel = false;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i] != 40 || pixels[i + 1] != 40 || pixels[i + 2] != 40)
            {
                anyDrawnPixel = true;
                break;
            }
        }
        Assert.True(anyDrawnPixel, "Nothing was drawn into the render target.");

        GraphicalUiElement? hit = renderer.HitTest(160, 120);
        Assert.NotNull(hit);
        Assert.False(string.IsNullOrEmpty(renderer.LoadedElementName));
    }

    [Fact]
    public void Resize_ChangesBufferSize_AndZoomScalesCanvas()
    {
        SpikeOptions options = SpikeOptions.Parse("test", Array.Empty<string>());

        using GumCanvasGame renderer = new GumCanvasGame(options.ProjectPath, options.ElementName);
        renderer.Resize(100, 50);
        renderer.RenderFrame();
        Assert.Equal(100 * 50 * 4, renderer.PixelBuffer.Length);

        renderer.Zoom = 2f;
        renderer.Resize(200, 100);
        renderer.RenderFrame();
        Assert.Equal(200 * 100 * 4, renderer.PixelBuffer.Length);
        Assert.Equal(2f, renderer.Zoom);
    }
}
