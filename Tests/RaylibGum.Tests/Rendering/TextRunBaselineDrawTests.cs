using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A styled run drawn with a swapped font sits on the line's shared baseline using that font's own baseline,
/// as MonoGame aligns each run by <c>runScale * runFont.BaselineY</c>. The base font (a 48px TTF) and the
/// swap font (an 18px .fnt) have different baseline proportions, so scaling the base font's baseline
/// to the run's size puts the swapped 'H' on the wrong row.
/// </summary>
public class TextRunBaselineDrawTests : BaseTestClass
{
    private const int CanvasWidth = 160;
    private const int CanvasHeight = 80;

    [Fact]
    public void Draw_RunWithSwappedFont_SitsOnTheBaseFontsBaseline()
    {
        string fontsDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts");
        string fntDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        Font baseFont = LoadFontEx(Path.Combine(fontsDirectory, "Orbitron-Black.ttf"), 48, null, 0);
        Texture2D fntPage = LoadTexture(Path.Combine(fntDirectory, "Font18Arial_0.png"));
        Font swapFont = ContentLoader.BuildFontFromFntText(File.ReadAllText(Path.Combine(fntDirectory, "Font18Arial.fnt")), fntPage);
        try
        {
            ContainerRuntime container = new();
            container.Width = CanvasWidth;
            container.Height = CanvasHeight;
            container.IsRenderTarget = true;

            TextRuntime runtime = new();
            runtime.X = 0;
            runtime.Y = 0;
            runtime.Width = CanvasWidth;
            runtime.Text = "HH";
            container.Children.Add(runtime);
            Text text = (Text)runtime.RenderableComponent!;
            text.Font = baseFont;
            text.InlineVariables.Add(new InlineVariable { VariableName = "BitmapFont", StartIndex = 1, CharacterCount = 1, Value = swapFont });

            Gum.GumService.Default.Root.Children!.Add(container);
            Gum.GumService.Default.Root.UpdateLayout();
            BeginDrawing();
            Gum.GumService.Default.Draw();
            EndDrawing();

            RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
            Image image = LoadImageFromTexture(rt.Texture);
            int splitX = (int)MeasureTextEx(baseFont, "H", 48, 0).X;
            int baseBottom = LowestInkRow(image, rt.Texture.Height, 0, splitX);
            int swapBottom = LowestInkRow(image, rt.Texture.Height, splitX, CanvasWidth);
            UnloadImage(image);
            Gum.GumService.Default.Root.Children.Remove(container);

            baseBottom.ShouldBeGreaterThan(0, "sanity check: the base-font 'H' must draw");
            swapBottom.ShouldBe(baseBottom, "the swapped-font 'H' must end on the same baseline row");
        }
        finally
        {
            RaylibFontMetricsRegistry.Remove(fntPage.Id);
            UnloadTexture(fntPage);
            UnloadFont(baseFont);
        }
    }

    private static int LowestInkRow(Image image, int textureHeight, int startX, int endX)
    {
        int lowest = -1;
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                if (GetImageColor(image, x, textureHeight - 1 - y).A > 128)
                {
                    lowest = y;
                    break;
                }
            }
        }
        return lowest;
    }
}
