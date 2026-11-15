using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Helpers;

/// <summary>
/// Renders Gum content into an off-screen render target and reads the pixels back (#7528). Nothing
/// is shown: the draw goes to a <see cref="RenderTexture2D"/> on the assembly's hidden raylib window
/// (opened in <see cref="TestAssemblyInitialize"/>), so it needs no extra window or OS input.
/// </summary>
public static class RaylibPixelHarness
{
    /// <summary>
    /// Fills a <paramref name="width"/> x <paramref name="height"/> render-target container with
    /// <paramref name="backdrop"/>, lets <paramref name="populate"/> add children to it, draws one
    /// frame, and returns the baked pixels. The container is removed from the Root afterwards.
    /// </summary>
    public static PixelGrid Render(int width, int height, Color backdrop, Action<ContainerRuntime> populate)
    {
        ContainerRuntime container = new();
        container.Width = width;
        container.Height = height;
        container.IsRenderTarget = true;

        RectangleRuntime background = new();
        background.Width = width;
        background.Height = height;
        background.IsFilled = true;
        background.StrokeWidth = 0;
        background.FillColor = backdrop;
        container.Children.Add(background);

        populate(container);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        try
        {
            BeginDrawing();
            Gum.GumService.Default.Draw();
            EndDrawing();

            RenderTexture2D target = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
            return Read(target, width, height);
        }
        finally
        {
            Gum.GumService.Default.Root.Children.Remove(container);
        }
    }

    /// <summary>One pixel of a baked render texture in top-left-origin draw space (x right, y down).</summary>
    public static Color ReadPixel(RenderTexture2D target, int x, int y)
    {
        Image image = LoadImageFromTexture(target.Texture);
        try
        {
            return GetImageColor(image, x, target.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    /// <summary>The pixel at (Width / 2, Height / 2) of the texture's rows as stored, without the vertical flip.</summary>
    public static Color ReadRawCenter(RenderTexture2D target)
    {
        Image image = LoadImageFromTexture(target.Texture);
        try
        {
            return GetImageColor(image, target.Texture.Width / 2, target.Texture.Height / 2);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    /// <summary>The center pixel in top-left-origin draw space.</summary>
    public static Color ReadCenter(RenderTexture2D target) =>
        ReadPixel(target, target.Texture.Width / 2, target.Texture.Height / 2);

    /// <summary>Reads the top-left width x height of a baked render texture in draw space.</summary>
    public static PixelGrid Read(RenderTexture2D target, int width, int height)
    {
        Image image = LoadImageFromTexture(target.Texture);
        try
        {
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = GetImageColor(image, x, target.Texture.Height - 1 - y);
                }
            }
            return new PixelGrid(width, height, pixels);
        }
        finally
        {
            UnloadImage(image);
        }
    }
}
