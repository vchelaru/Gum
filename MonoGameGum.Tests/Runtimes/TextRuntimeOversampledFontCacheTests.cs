using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Tests.Hot;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #7157: with no IInMemoryFontCreator (or one that returns null), oversampling serves the
// raster size from the project's pre-built FontCache instead of leaving the native font blown up.
public class TextRuntimeOversampledFontCacheTests : BaseTestClass
{
    private const int PrebuiltLineHeight = 40;

    private readonly bool _savedUseFontOversampling = TextRuntime.UseFontOversampling;
    private readonly IInMemoryFontCreator? _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
    private readonly string _contentFolder;
    private readonly List<TextRuntime> _texts = new();

    public TextRuntimeOversampledFontCacheTests()
    {
        TextRuntime.UseFontOversampling = true;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

        _contentFolder = Path.Combine(Path.GetTempPath(), "gum-7157-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentFolder);
        global::ToolsUtilities.FileManager.RelativeDirectory = _contentFolder + "/";
    }

    public override void Dispose()
    {
        // The shared oversampled fonts are static and keyed by name, so texts still using one would
        // hand it to the next test. Turning oversampling off puts each text back on its native font.
        TextRuntime.PrebuiltFontClock = () => DateTime.UtcNow;
        TextRuntime.UseFontOversampling = false;
        foreach (TextRuntime textRuntime in _texts)
        {
            textRuntime.UpdateAutomaticFontOversampling(1f);
        }

        TextRuntime.UseFontOversampling = _savedUseFontOversampling;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        base.Dispose();
        try
        {
            Directory.Delete(_contentFolder, recursive: true);
        }
        catch (IOException)
        {
            // A texture can still hold a file open; the temp folder is not worth failing a test over.
        }
    }

    // FontSize 20 oversampled at 2x rasterizes at 40, so the project's FontCache holds Font40Pixel.
    private void BuildPrebuiltSize40() =>
        PreviewAccuracy2World.WriteFont(_contentFolder, "Font40Pixel", advance: 11, lineHeight: PrebuiltLineHeight);

    private TextRuntime CreateText()
    {
        TextRuntime textRuntime = new();
        textRuntime.Font = "Pixel";
        textRuntime.FontSize = 20;
        _texts.Add(textRuntime);
        return textRuntime;
    }

    private static Text GetText(TextRuntime textRuntime) => (Text)textRuntime.RenderableComponent!;

    [Fact]
    public void RegenerateOversampledFont_NoCreatorAndSizeInFontCache_UsesThePrebuiltFont()
    {
        BuildPrebuiltSize40();
        TextRuntime textRuntime = CreateText();

        bool result = textRuntime.RegenerateOversampledFont(2f);

        result.ShouldBeTrue();
        Text text = GetText(textRuntime);
        text.BitmapFont!.LineHeightInPixels.ShouldBe(PrebuiltLineHeight);
        text.OversampleCompensationScale.ShouldBe(20f / 40f);
    }

    [Fact]
    public void RegenerateOversampledFont_NoCreatorAndSizeNotInFontCache_KeepsTheNativeFont()
    {
        TextRuntime textRuntime = CreateText();
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;

        bool result = textRuntime.RegenerateOversampledFont(2f);

        result.ShouldBeFalse();
        text.BitmapFont.ShouldBeSameAs(nativeFont);
        text.OversampleCompensationScale.ShouldBe(1f);
    }

    [Fact]
    public void RegenerateOversampledFont_CreatorReturnsNullAndSizeInFontCache_UsesThePrebuiltFont()
    {
        BuildPrebuiltSize40();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new NullFontCreator();
        TextRuntime textRuntime = CreateText();

        bool result = textRuntime.RegenerateOversampledFont(2f);

        result.ShouldBeTrue();
        GetText(textRuntime).BitmapFont!.LineHeightInPixels.ShouldBe(PrebuiltLineHeight);
    }

    [Fact]
    public void RegenerateOversampledFont_CreatorBuildsTheFont_TheFontCacheIsNotUsed()
    {
        BuildPrebuiltSize40();
        BitmapFont createdFont = new BitmapFont((Texture2D)null!, StubFontData);
        createdFont.SetFontPattern(256, 256);
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new FixedFontCreator(createdFont);
        TextRuntime textRuntime = CreateText();

        textRuntime.RegenerateOversampledFont(2f).ShouldBeTrue();

        GetText(textRuntime).BitmapFont.ShouldBeSameAs(createdFont);
    }

    [Fact]
    public void RegenerateOversampledFont_OversamplingOff_DoesNotUseTheFontCache()
    {
        BuildPrebuiltSize40();
        TextRuntime.UseFontOversampling = false;
        TextRuntime textRuntime = CreateText();

        textRuntime.RegenerateOversampledFont(2f).ShouldBeFalse();
    }

    [Fact]
    public void RegenerateOversampledFont_TwoTextsAtTheSameSize_ShareOnePrebuiltFont()
    {
        BuildPrebuiltSize40();
        TextRuntime first = CreateText();
        TextRuntime second = CreateText();

        first.RegenerateOversampledFont(2f).ShouldBeTrue();
        second.RegenerateOversampledFont(2f).ShouldBeTrue();

        GetText(second).BitmapFont.ShouldBeSameAs(GetText(first).BitmapFont);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_NoCreatorAndSizeInFontCache_UsesThePrebuiltFont()
    {
        BuildPrebuiltSize40();
        TextRuntime textRuntime = CreateText();

        bool result = textRuntime.UpdateAutomaticFontOversampling(2f);

        result.ShouldBeTrue();
        GetText(textRuntime).BitmapFont!.LineHeightInPixels.ShouldBe(PrebuiltLineHeight);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_TurnedOffAfterUsingThePrebuiltFont_RevertsToTheNativeFont()
    {
        BuildPrebuiltSize40();
        TextRuntime textRuntime = CreateText();
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;
        textRuntime.UpdateAutomaticFontOversampling(2f).ShouldBeTrue();

        TextRuntime.UseFontOversampling = false;
        textRuntime.UpdateAutomaticFontOversampling(2f).ShouldBeFalse();

        text.BitmapFont.ShouldBeSameAs(nativeFont);
        text.OversampleCompensationScale.ShouldBe(1f);
    }

    [Theory]
    [InlineData("")]
    [InlineData("this is not a font file")]
    public void RegenerateOversampledFont_CorruptFntInFontCache_KeepsTheNativeFontAndReportsTheError(string contents)
    {
        Directory.CreateDirectory(Path.Combine(_contentFolder, "FontCache"));
        File.WriteAllText(Path.Combine(_contentFolder, "FontCache", "Font40Pixel.fnt"), contents);
        TextRuntime textRuntime = CreateText();
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;
        List<string> errors = new();
        Action<string> onError = errors.Add;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += onError;
        try
        {
            bool result = false;
            Should.NotThrow(() => result = textRuntime.RegenerateOversampledFont(2f));

            result.ShouldBeFalse();
            text.BitmapFont.ShouldBeSameAs(nativeFont);
            errors.ShouldContain(error => error.Contains("Font40Pixel.fnt"));
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= onError;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_SizeMissingFromFontCache_ProbesOncePerSecondNotOncePerCall()
    {
        DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TextRuntime.PrebuiltFontClock = () => now;
        int probes = 0;
        global::ToolsUtilities.FileManager.CustomGetStreamFromFile = name =>
        {
            if (name.EndsWith("Font40Pixel.fnt"))
            {
                probes++;
            }
            return null!;
        };
        TextRuntime first = CreateText();
        TextRuntime second = CreateText();

        for (int frame = 0; frame < 5; frame++)
        {
            first.RegenerateOversampledFont(2f);
            second.RegenerateOversampledFont(2f);
        }

        probes.ShouldBe(1);

        now = now.AddSeconds(1.5);
        first.RegenerateOversampledFont(2f);
        second.RegenerateOversampledFont(2f);

        probes.ShouldBe(2);
    }

    [Fact]
    public void RegenerateOversampledFont_SizeAppearsInFontCacheAfterTheRetryDelay_IsUsed()
    {
        DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TextRuntime.PrebuiltFontClock = () => now;
        TextRuntime textRuntime = CreateText();
        textRuntime.RegenerateOversampledFont(2f).ShouldBeFalse();

        BuildPrebuiltSize40();
        textRuntime.RegenerateOversampledFont(2f).ShouldBeFalse("because the miss is remembered for a second");
        now = now.AddSeconds(1.5);

        textRuntime.RegenerateOversampledFont(2f).ShouldBeTrue();
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""StubFont_0.png""
chars count=1
char id=65   x=0   y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";

    private sealed class NullFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave) => null;
    }

    private sealed class FixedFontCreator : IInMemoryFontCreator
    {
        private readonly BitmapFont _font;
        public FixedFontCreator(BitmapFont font) => _font = font;
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave) => _font;
    }
}
