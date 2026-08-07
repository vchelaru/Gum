using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Regression coverage for issue #4330's manual-test finding: on the Silk.NET/Skia backend, setting
/// <c>Camera.Zoom</c> to a non-1 value and rendering more than one frame made the on-screen content
/// grow unboundedly every frame with no further input -- confirmed by the user as a live "zooms in
/// further and further until everything is off screen" runaway. <see cref="RenderingLibrary.Graphics.Renderer"/>'s
/// top-level Draw pass calls <c>canvas.Scale(Camera.Zoom)</c>/<c>canvas.Translate(...)</c> directly on
/// the persistent <c>SKCanvas</c> with no <c>canvas.Save()</c>/<c>canvas.Restore()</c> bracketing --
/// unlike a fresh matrix passed per frame (XNALIKE's SpriteBatch.Begin) or an explicit begin/end pair
/// (raylib's BeginMode2D/EndMode2D), each frame's Scale/Translate compounds onto whatever the
/// PREVIOUS frame already applied, since nothing ever resets the canvas's transform in between.
/// </summary>
public class RendererCameraZoomTests
{
    public RendererCameraZoomTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void ConsecutiveFrames_WithNonOneCameraZoom_DoNotAccumulateScale()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        SystemManagers.Default.Renderer.Camera.Zoom = 2f;

        RectangleRuntime rectangle = new()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        GumService.Default.Root.Children.Add(rectangle);

        // At the correct 2x zoom the rectangle covers screen pixels 0-20, so (25, 25) stays
        // background. If Scale/Translate compound every frame (the bug), the SECOND frame's
        // effective zoom becomes 4x (covering 0-40), which would turn this pixel red.
        for (int frame = 1; frame <= 3; frame++)
        {
            surface.Canvas.Clear(SKColors.Black);
            GumService.Default.Draw();

            using SKImage image = surface.Snapshot();
            using SKBitmap bitmap = SKBitmap.FromImage(image);

            bitmap.GetPixel(25, 25).ShouldBe(SKColors.Black,
                $"because frame {frame} must render at the same 2x zoom as every other frame, not an accumulated one");
        }
    }
}
