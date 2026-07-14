using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum;
using SkiaSharp;
using System;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pixel-level regression coverage for the <c>[Custom=Name]</c> per-letter callback (issue #3692):
/// per-glyph position/color has no RichTextKit <see cref="Topten.RichTextKit.Style"/> field to assert
/// on (see <see cref="GoldenImageAssert"/>'s remarks), so this renders the same "Wave" callback the
/// MonoGame/Raylib text samples use and diffs the actual pixels against an approved baseline.
/// </summary>
public class TextCustomizationGoldenImageTests
{
    public TextCustomizationGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void CustomTag_WavyRainbowText_MatchesGoldenBaseline()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(220, 60));
        GumService.Default.Initialize(surface.Canvas, 220, 60);

        Text.Customizations["Wave"] = (int index, string block) => new LetterCustomization
        {
            YOffset = MathF.Sin(index * 0.9f) * 10f,
            Color = System.Drawing.Color.FromArgb(
                255,
                (int)(128 + 127 * MathF.Sin(index * 0.7f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 2f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 4f))),
        };

        TextRuntime text = new()
        {
            X = 4,
            Y = 20,
            Font = "Arial",
            FontSize = 24,
            Text = "[Custom=Wave]Wavy rainbow text[/Custom]",
        };
        GumService.Default.Root.Children.Add(text);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "Text_CustomWavyRainbow");
    }
}
