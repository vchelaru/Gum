using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum;
using SkiaSharp;
using System;
using System.IO;
using Topten.RichTextKit;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pixel-level regression coverage for the <c>[Custom=Name]</c> per-letter callback (issue #3692):
/// per-glyph position/color has no RichTextKit <see cref="Topten.RichTextKit.Style"/> field to assert
/// on (see <see cref="GoldenImageAssert"/>'s remarks), so this renders the same "Wave" callback the
/// MonoGame/Raylib text samples use and diffs the actual pixels against an approved baseline.
///
/// Renders through a bundled font (<see cref="FixedFontMapper"/>) rather than a system font family like
/// "Arial" -- text glyph shapes depend on whatever font is actually installed on the machine, which is
/// not reproducible across CI runners (the macOS Actions image has no Arial and silently substitutes a
/// different typeface, which blew the pixel tolerance the first time this test ran in CI).
/// </summary>
public class TextCustomizationGoldenImageTests : IDisposable
{
    /// <summary>
    /// Always resolves to the one bundled <see cref="SKTypeface"/> regardless of the requested
    /// <see cref="IStyle.FontFamily"/>, so a golden-image test's glyph shapes don't depend on which
    /// fonts happen to be installed on the machine running the test.
    /// </summary>
    private sealed class FixedFontMapper : FontMapper
    {
        private readonly SKTypeface _typeface;

        public FixedFontMapper(SKTypeface typeface) => _typeface = typeface;

        public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants) => _typeface;
    }

    private readonly FontMapper _originalFontMapper;
    private readonly SKTypeface _testTypeface;

    public TextCustomizationGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;

        string fontPath = Path.Combine(AppContext.BaseDirectory, "GoldenImages", "Fonts", "DejaVuSansMono.ttf");
        _testTypeface = SKTypeface.FromFile(fontPath)
            ?? throw new InvalidOperationException($"Failed to load bundled test font at '{fontPath}'.");

        // RichTextKit resolves any TextBlock that doesn't set an explicit FontMapper (every TextBlock
        // SkiaGum.Text builds) through this static default -- swapping it here makes every glyph in this
        // test render from the bundled font instead of whatever "Arial" resolves to on this machine.
        _originalFontMapper = FontMapper.Default;
        FontMapper.Default = new FixedFontMapper(_testTypeface);
    }

    public void Dispose()
    {
        FontMapper.Default = _originalFontMapper;
        _testTypeface.Dispose();
    }

    [Fact]
    public void CustomTag_WavyRainbowText_MatchesGoldenBaseline()
    {
        // Wide enough for "Wavy rainbow text" at FontSize 24 in the bundled monospace test font, which is
        // noticeably wider per character than the "Arial" this used to (unreliably) render with.
        using SKSurface surface = SKSurface.Create(new SKImageInfo(320, 60));
        GumService.Default.Initialize(surface.Canvas, 320, 60);

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
            // FontName is irrelevant here -- FixedFontMapper ignores it and always resolves to the
            // bundled test typeface. Set for realism/documentation only.
            Font = "Arial",
            FontSize = 24,
            Text = "[Custom=Wave]Wavy rainbow text[/Custom]",
        };
        GumService.Default.Root.Children.Add(text);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "Text_CustomWavyRainbow");
    }
}
