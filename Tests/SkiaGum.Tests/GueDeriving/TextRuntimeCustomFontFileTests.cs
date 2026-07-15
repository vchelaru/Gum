using Gum;
using Gum.GueDeriving;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.IO;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Pins #3670/#3703: TextRuntime.CustomFontFile pointing at a .ttf resolves through
/// GumFontMapper to a loaded SKTypeface, instead of Skia's previous total no-op (FontName
/// was set from Font only; UseCustomFont/CustomFontFile were never read).
/// </summary>
public class TextRuntimeCustomFontFileTests
{
    private static readonly string FixtureTtfPath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf");

    private static void EnsureInitialized()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);
    }

    [Fact]
    public void SettingCustomFontFileToTtf_DirectProperty_ResolvesToRegisteredFamily()
    {
        EnsureInitialized();

        TextRuntime sut = new();
        sut.UseCustomFont = true;
        sut.CustomFontFile = FixtureTtfPath;

        Text containedText = (Text)sut.RenderableComponent;
        containedText.FontName.ShouldBe(SkiaGum.Content.Fonts.GumFontMapper.RegisterFontFile(FixtureTtfPath));
    }

    [Fact]
    public void SettingCustomFontFileToTtf_ViaSetProperty_ResolvesToRegisteredFamily()
    {
        EnsureInitialized();

        TextRuntime sut = new();
        sut.SetProperty(nameof(TextRuntime.UseCustomFont), true);
        sut.SetProperty(nameof(TextRuntime.CustomFontFile), FixtureTtfPath);

        Text containedText = (Text)sut.RenderableComponent;
        containedText.FontName.ShouldBe(SkiaGum.Content.Fonts.GumFontMapper.RegisterFontFile(FixtureTtfPath));
    }
}
