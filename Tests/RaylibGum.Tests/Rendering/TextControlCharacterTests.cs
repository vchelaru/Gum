using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6835: MonoGame's BitmapFont draws a control character as the space glyph (blank), a tab as four
/// spaces, and a newline with no advance. Raylib's native text calls substitute '?' for a glyph the
/// font lacks, so control characters must be mapped to those same blanks before measuring and drawing.
/// </summary>
public class TextControlCharacterTests : BaseTestClass
{
    private const int CanvasWidth = 200;
    private const int CanvasHeight = 60;

    [Theory]
    [InlineData("a\rb", "a b")]
    [InlineData("a\u0001b", "a b")]
    [InlineData("a\u001fb", "a b")]
    [InlineData("a\tb", "a    b")]
    [InlineData("a\r", "a ")]
    public void Measure_ControlCharacter_MatchesTheBlankMonoGameUses(string withControl, string blank)
    {
        (float Direct, float Layout) actual = MeasuredWidth(withControl);
        (float Direct, float Layout) expected = MeasuredWidth(blank);

        actual.Direct.ShouldBe(expected.Direct, 0.01f);
        actual.Layout.ShouldBe(expected.Layout, 0.01f);
    }

    [Theory]
    [InlineData("a\rb", "a b")]
    [InlineData("a\u0001b", "a b")]
    [InlineData("a\tb", "a    b")]
    [InlineData("[Color=Red]a\rb[/Color]", "[Color=Red]a b[/Color]")]
    public void Draw_ControlCharacter_DrawsLikeTheBlank(string withControl, string blank)
    {
        Color[] actual = Render(withControl);
        Color[] expected = Render(blank);

        expected.Count(p => p.A > 0).ShouldBeGreaterThan(0);
        for (int i = 0; i < actual.Length; i++)
        {
            (actual[i].R, actual[i].G, actual[i].B, actual[i].A)
                .ShouldBe((expected[i].R, expected[i].G, expected[i].B, expected[i].A),
                    $"pixel ({i % CanvasWidth},{i / CanvasWidth}) of '{withControl}' should match '{blank}'");
        }
    }

    private static (float Direct, float Layout) MeasuredWidth(string content)
    {
        TextRuntime runtime = new();
        runtime.Text = content;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        float direct = ((Text)runtime.RenderableComponent!).MeasureString(content);
        float layout = runtime.GetAbsoluteWidth();
        Gum.GumService.Default.Root.Children.Remove(runtime);
        return (direct, layout);
    }

    private static Color[] Render(string markup)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.X = 2;
        text.Y = 2;
        text.Text = markup;
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
