using RenderingLibrary;
using Shouldly;
using SkiaGum.Content.Fonts;
using SkiaSharp;
using System;
using System.IO;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A Text whose font family, weight, slant or embedded font changes between frames draws what a fresh Text draws, so
/// GumFontMapper's resolved-typeface cache never serves a stale typeface (#7846).
/// </summary>
public class TextFontResolutionTests : BaseTestClass
{
    private const string SampleText = "Hello [IsBold=true]bold[/IsBold] and [IsItalic=true]italic[/IsItalic] text wrapped";

    private static readonly string FixtureTtfPath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf");

    private static Text CreateText(string fontName, bool isItalic = false, float boldWeight = 1)
    {
        Text text = new Text();
        text.RawText = SampleText;
        text.FontSize = 20;
        text.Width = 160;
        text.Height = 60;
        text.FontName = fontName;
        text.IsItalic = isItalic;
        text.BoldWeight = boldWeight;
        return text;
    }

    private static byte[] Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(240, 100));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    private static void ShouldMatchFresh(Text changed, Text fresh, string because)
    {
        Render(changed).ShouldBe(Render(fresh), because);
        changed.GetCachedTextBlock().Lines.Count.ShouldBe(fresh.GetCachedTextBlock().Lines.Count, because);
    }

    [Fact]
    public void Render_FontFamilyBoldAndItalicChangedBetweenFrames_MatchesFresh()
    {
        Text changed = CreateText("Arial");
        Render(changed);

        changed.FontName = "Courier New";
        ShouldMatchFresh(changed, CreateText("Courier New"), "family changed");

        changed.BoldWeight = 1.5f;
        ShouldMatchFresh(changed, CreateText("Courier New", boldWeight: 1.5f), "bold on");

        changed.IsItalic = true;
        ShouldMatchFresh(changed, CreateText("Courier New", isItalic: true, boldWeight: 1.5f), "italic on");

        changed.BoldWeight = 1;
        changed.IsItalic = false;
        changed.FontName = "Arial";
        ShouldMatchFresh(changed, CreateText("Arial"), "back to regular Arial");
    }

    [Fact]
    public void Render_EmbeddedFontRegisteredAfterFamilyWasDrawn_DrawsTheEmbeddedFont()
    {
        string family = "TextFontResolutionEmbeddedFamily";
        Text changed = CreateText(family);
        byte[] beforeRegistration = Render(changed);

        GumFontMapper.RegisterFont(family, File.ReadAllBytes(FixtureTtfPath)).ShouldNotBeNull();
        changed.RawText = SampleText + " again";
        Text fresh = CreateText(family);
        fresh.RawText = SampleText + " again";

        ShouldMatchFresh(changed, fresh, "embedded font registered");
        Render(fresh).ShouldNotBe(beforeRegistration);
    }

    [Fact]
    public void TypefaceFromStyle_SystemFamilyResolvedTwice_ReturnsTheSameLiveTypeface()
    {
        GumFontMapper mapper = new();
        Style style = new() { FontFamily = "Arial", FontWeight = 700, FontItalic = true };

        SKTypeface first = mapper.TypefaceFromStyle(style, ignoreFontVariants: false);
        SKTypeface second = mapper.TypefaceFromStyle(style, ignoreFontVariants: false);

        second.ShouldBeSameAs(first);
        second.Handle.ShouldNotBe(IntPtr.Zero);
    }

    [Fact]
    public void TypefaceFromStyle_EmbeddedFontReRegistered_ReturnsTheNewTypeface()
    {
        string family = "TextFontResolutionReRegisteredFamily";
        byte[] fontBytes = File.ReadAllBytes(FixtureTtfPath);
        GumFontMapper mapper = new();
        Style style = new() { FontFamily = family, FontWeight = 700 };
        mapper.TypefaceFromStyle(style, ignoreFontVariants: false);

        SKTypeface? firstBold = GumFontMapper.RegisterFont(family, fontBytes, style: "bold");
        mapper.TypefaceFromStyle(style, ignoreFontVariants: false).ShouldBeSameAs(firstBold);

        SKTypeface? secondBold = GumFontMapper.RegisterFont(family.ToUpperInvariant(), fontBytes, style: "Bold");
        mapper.TypefaceFromStyle(style, ignoreFontVariants: false).ShouldBeSameAs(secondBold);
    }
}
