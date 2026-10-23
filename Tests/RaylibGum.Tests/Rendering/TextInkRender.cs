using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Draws one TextRuntime into a render target and reads the pixels back, for text draw tests that assert on
/// where the ink lands.
/// </summary>
internal static class TextInkRender
{
    public const int CanvasWidth = 240;
    public const int CanvasHeight = 240;

    public readonly record struct Bounds(float Left, float Top, float Right, float Bottom)
    {
        public float Width => Right - Left;
        public float Height => Bottom - Top;
    }

    public static Bounds InkBounds(Color[] pixels)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].A > 128)
            {
                int x = i % CanvasWidth;
                int y = i / CanvasWidth;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
            }
        }
        left.ShouldNotBe(int.MaxValue, "because something is drawn");
        return new Bounds(left, top, right, bottom);
    }

    /// <summary>
    /// Renders <paramref name="markup"/> at FontScale 2 with its top-left at (<paramref name="left"/>,
    /// <paramref name="top"/>), after <paramref name="configure"/> sets anything else on the runtime.
    /// </summary>
    public static Color[] Render(string markup, float left, float top, Action<TextRuntime>? configure = null)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.X = left;
        text.Y = top;
        text.FontScale = 2;
        text.Text = markup;
        configure?.Invoke(text);
        container.Children.Add(text);

        Gum.GumService.Default.Root.Children!.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        Color[] result = new Color[CanvasWidth * CanvasHeight];
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = 0; x < CanvasWidth; x++)
            {
                result[y * CanvasWidth + x] = GetImageColor(image, x, rt.Texture.Height - 1 - y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Remove(container);
        return result;
    }
}
