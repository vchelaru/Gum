using Shouldly;
using SkiaGum;
using SkiaSharp;
using System.IO;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Pins Text.Typeface (issue #3708): the explicit-font-object escape hatch Skia lacked entirely,
/// mirroring XNALIKE's BitmapFont / Raylib's Typeface. Setting it bypasses FontName-based
/// resolution -- GetStyle().FontFamily resolves to a GumFontMapper-registered key for the
/// assigned SKTypeface instead of the plain FontName string.
/// </summary>
public class TextTypefaceTests
{
    private static readonly string FixtureTtfPath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf");

    [Fact]
    public void Typeface_DefaultsToNull()
    {
        Text sut = new();

        sut.Typeface.ShouldBeNull();
    }

    [Fact]
    public void Typeface_Null_StyleFontFamilyUsesFontName()
    {
        Text sut = new();
        sut.FontName = "Arial";

        sut.GetStyle().FontFamily.ShouldBe("Arial");
    }

    [Fact]
    public void Typeface_SetExplicitly_StyleFontFamilyResolvesToRegisteredTypefaceKey()
    {
        Text sut = new();
        sut.FontName = "Arial";
        SKTypeface typeface = SKTypeface.FromFile(FixtureTtfPath);

        sut.Typeface = typeface;

        sut.GetStyle().FontFamily.ShouldBe(
            SkiaGum.Content.Fonts.GumFontMapper.RegisterTypeface(typeface));
    }
}
