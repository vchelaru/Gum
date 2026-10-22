using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6439: the distance between drawn lines must follow FontScale, and the Text's reported height must
/// agree with it. Renders "x\nx" and measures the drawn distance between the two glyph tops.
/// </summary>
public class TextLineAdvanceDrawTests : BaseTestClass
{
    private const int CanvasWidth = 200;
    private const int CanvasHeight = 400;

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(2f, 1.5f)]
    public void Draw_TwoPlainLines_AreSpacedByTheReportedLineAdvance(float fontScale, float multiplier)
    {
        float advance = Measure(fontScale, multiplier, out float height, out Text text);

        advance.ShouldBe(text.GetLineTopOffsets()[1], 1.5f, "because the second line is drawn at its offset");
        height.ShouldBe(2 * text.GetLineTopOffsets()[1], 1.5f, "because the reported height is two line advances");
    }

    [Fact]
    public void Draw_TwoPlainLines_FontScaleTwoDrawsTwiceAsFarApartAsFontScaleOne()
    {
        float atOne = Measure(1f, 1f, out _, out _);
        float atTwo = Measure(2f, 1f, out _, out _);

        atTwo.ShouldBe(2 * atOne, 2f);
    }

    private static float Measure(float fontScale, float multiplier, out float height, out Text text)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime runtime = new();
        runtime.X = 2;
        runtime.Y = 2;
        runtime.Text = "x\nx";
        runtime.FontScale = fontScale;
        runtime.LineHeightMultiplier = multiplier;
        container.Children.Add(runtime);

        Gum.GumService.Default.Root.Children!.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        List<int> inkRows = new();
        for (int y = 0; y < CanvasHeight; y++)
        {
            bool hasInk = false;
            for (int x = 0; x < CanvasWidth && !hasInk; x++)
            {
                hasInk = GetImageColor(image, x, rt.Texture.Height - 1 - y).A > 0;
            }
            if (hasInk)
            {
                inkRows.Add(y);
            }
        }
        UnloadImage(image);

        height = runtime.GetAbsoluteHeight();
        text = (Text)runtime.RenderableComponent!;
        Gum.GumService.Default.Root.Children.Remove(container);

        int firstTop = inkRows[0];
        int secondTop = inkRows.First(row => row > firstTop && !inkRows.Contains(row - 1));
        return secondTop - firstTop;
    }
}
