using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback test proving <see cref="CircleRuntime.Blend"/> actually reaches raylib's
/// blend state on the circle's fill/stroke passes (issue #3491), rather than being a dead
/// round-trip. Uses the same baked-render-target harness as <see cref="BlendModeRenderTests"/> —
/// the composite is what makes the blend observable headlessly.
/// </summary>
public class CircleBlendRenderTests : BaseTestClass
{
    private static Color ReadRenderTargetCenter(RenderTexture2D renderTexture)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, renderTexture.Texture.Width / 2, renderTexture.Texture.Height / 2);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    [Fact]
    public void Draw_AdditiveBlendCircle_AddsFillColorOntoBackgroundInsteadOfOverwriting()
    {
        const int size = 32;

        ContainerRuntime container = new();
        container.Width = size;
        container.Height = size;
        container.IsRenderTarget = true;

        // Opaque blue background fills the cell.
        RectangleRuntime background = new();
        background.Width = size;
        background.Height = size;
        background.IsFilled = true;
        background.FillColor = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        container.Children.Add(background);

        // Opaque red circle drawn additively over the blue. Additive keeps the destination
        // (blue) and adds the source (red), so the center reads as magenta (R and B both high).
        // Without the blend wrap the circle draws with straight alpha — an opaque red overwrite,
        // leaving blue near zero — which is exactly what this asserts against.
        CircleRuntime circle = new();
        circle.Width = size;
        circle.Height = size;
        circle.IsFilled = true;
        circle.FillColor = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        circle.Blend = Blend.Additive;
        container.Children.Add(circle);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value);

        GumService.Default.Root.Children.Clear();

        center.R.ShouldBeGreaterThan((byte)150);
        center.B.ShouldBeGreaterThan((byte)150);
    }
}
