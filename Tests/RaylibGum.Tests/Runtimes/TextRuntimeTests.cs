using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Raylib_cs;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace RaylibGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{
    public TextRuntimeTests()
    {
        // not sure if this can run on github actions:
        if (!Raylib.IsWindowReady())
        {
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(800, 600, "Test Window");
        }
    }

    #region AbsoluteWidth

    [Fact]
    public void AbsoluteWidth_ShouldBeChangedByText_IfRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Text = "Short";
        float shortWidth = sut.GetAbsoluteWidth();

        sut.Text = "This is much longer";
        float longWidth = sut.GetAbsoluteWidth();

        longWidth.ShouldBeGreaterThan(shortWidth);
    }

    [Fact]
    public void AbsoluteWidth_ShouldNotIncludeNewlines()
    {
        TextRuntime textRuntime = new();

        textRuntime.Text = "Hello";

        var widthBefore = textRuntime.GetAbsoluteWidth();

        textRuntime.Text = "Hello\na";

        var widthAfter = textRuntime.GetAbsoluteWidth();

        widthBefore.ShouldBe(widthAfter, "Because a trailing newline should not affect the width of a text");
    }

    #endregion

    #region AssignFontInConstructor

    [Fact]
    public void AssignFontInConstructor_WhenFalse_ShouldNotSetFont()
    {
        var saved = TextRuntime.AssignFontInConstructor;
        try
        {
            TextRuntime.AssignFontInConstructor = false;
            TextRuntime sut = new();
            sut.FontFamily.ShouldBeNullOrEmpty();
        }
        finally
        {
            TextRuntime.AssignFontInConstructor = saved;
        }
    }

    [Fact]
    public void AssignFontInConstructor_WhenTrue_ShouldSetDefaultFont()
    {
        var saved = TextRuntime.AssignFontInConstructor;
        try
        {
            TextRuntime.AssignFontInConstructor = true;
            TextRuntime sut = new();
            sut.FontFamily.ShouldBe(TextRuntime.DefaultFont);
        }
        finally
        {
            TextRuntime.AssignFontInConstructor = saved;
        }
    }

    #endregion

    #region CustomFontFile

    [Fact]
    public void CustomFontFile_ShouldBeNullByDefault()
    {
        TextRuntime sut = new();
        sut.CustomFontFile.ShouldBeNull();
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultFont_ShouldBeArial()
    {
        TextRuntime.DefaultFont.ShouldBe("Arial");
    }

    [Fact]
    public void DefaultFontSize_ShouldBe18()
    {
        TextRuntime.DefaultFontSize.ShouldBe(18);
    }

    #endregion

    #region Font Loading

    // #3093: font properties set through the direct C# setters (FontFamily/FontSize/...) must load
    // the font onto the renderable exactly as the string/ApplyState path does. Differential guard:
    // the two paths must produce an identical, laid-out Text — the same loaded atlas (proving a real
    // font loaded, not raylib's small built-in default) AND the same measured width (proving the
    // correct size, which an atlas/glyph count alone would not catch). The bug routed the direct
    // setters through Text.HandleUpdateFontValues, a stub that copied FontSize but never loaded the
    // font, so the direct-setter Text kept the default font.
    [Fact]
    public void Font_SetViaDirectProperties_ShouldLoadSameFontAsStatePath()
    {
        // Serve the gold Font18Arial fixture (and its page) purely in memory, keyed by file name,
        // so the (Arial, 18) cache file resolves regardless of disk layout. Mirrors
        // ContentLoaderTests' bundled-font hook.
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        Dictionary<string, byte[]> inMemoryFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Font18Arial.fnt", File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial.fnt")) },
            { "Font18Arial_0.png", File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial_0.png")) },
        };

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            // Unique (non-existent) relative directory: the font's cache key (the standardized
            // absolute path) becomes unique to this test run, so a prior test's cached
            // "Font18Arial.fnt" can't mask the result and resolution is forced through the in-memory
            // hook. Mirrors the GUID-path isolation in ContentLoaderTests.
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(),
                "GumRaylibDirectFontTest_" + Guid.NewGuid().ToString("N")).Replace('\\', '/') + "/";
            FileManager.CustomGetStreamFromFile = incomingPath =>
                inMemoryFiles.TryGetValue(Path.GetFileName(incomingPath), out byte[]? bytes)
                    ? new MemoryStream(bytes)
                    : null!;

            // Reference: the path that already works — font set through the string / state path.
            TextRuntime viaState = new();
            viaState.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            viaState.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            viaState.SetProperty("Font", "Arial");
            viaState.SetProperty("FontSize", 18);
            viaState.Text = "Hello World";

            // Subject: the identical font set through the direct C# property setters.
            TextRuntime viaSetter = new();
            viaSetter.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            viaSetter.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            viaSetter.FontFamily = "Arial";
            viaSetter.FontSize = 18;
            viaSetter.Text = "Hello World";

            var stateFont = ((Gum.Renderables.Text)viaState.RenderableComponent).Font;
            var setterFont = ((Gum.Renderables.Text)viaSetter.RenderableComponent).Font;

            // Absolute: the direct-setter path actually loaded the real Font18Arial atlas (256x256,
            // per ContentLoaderTests), not raylib's 128x128 built-in default. Asserting this on the
            // subject directly (not just "setter == state") means the test still fails if a future
            // regression broke BOTH paths down to the default font.
            setterFont.Texture.Width.ShouldBe(256);
            setterFont.Texture.Height.ShouldBe(256);
            // Differential: the direct-setter path matches the known-good string/state path on the
            // loaded atlas ...
            setterFont.Texture.Width.ShouldBe(stateFont.Texture.Width);
            setterFont.Texture.Height.ShouldBe(stateFont.Texture.Height);
            // ... and on measured size (catches a wrong font size, which atlas size alone would not).
            viaSetter.GetAbsoluteWidth().ShouldBe(viaState.GetAbsoluteWidth());
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // #3039: the string / state set path (SetProperty -> CustomSetPropertyOnRenderable.UpdateToFontValues)
    // must honor IsAllLayoutSuspended and DEFER the font load, exactly as the MonoGame/Tool path does, so
    // the documented font-batching optimization (docs/.../font-performance.md) is real on Raylib too.
    // Before the fix this path loaded eagerly on every setter, making the batch a no-op and "accidentally"
    // masking #2999. Note the font *property* setters already defer (they route through the shared
    // GraphicalUiElement.UpdateToFontValues, which sets IsFontDirty), so IsFontDirty alone cannot tell the
    // two builds apart — the discriminator is whether the eager LOAD happened. Detection: the load resolves
    // the font-cache file through FileManager.CustomGetStreamFromFile; a unique (non-existent) font name +
    // relative directory guarantees no cache hit and no on-disk file, so any request for that font during
    // suspension proves the (now-removed) eager load ran.
    [Fact]
    public void StringSetPath_ShouldDeferFontLoad_WhenIsAllLayoutSuspended()
    {
        string uniqueFontName = "GumDeferFontTest_" + Guid.NewGuid().ToString("N");

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        bool savedSuspended = GraphicalUiElement.IsAllLayoutSuspended;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            // Unique relative directory: the font's cache key (its standardized absolute path) becomes
            // unique to this run, so a prior test's cache can't mask the result and resolution is forced
            // through the in-memory hook. Mirrors the GUID-path isolation in ContentLoaderTests.
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(),
                "GumRaylibDeferFontTest_" + Guid.NewGuid().ToString("N")).Replace('\\', '/') + "/";

            int fontRequestCount = 0;
            FileManager.CustomGetStreamFromFile = incomingPath =>
            {
                if (incomingPath.Contains(uniqueFontName, StringComparison.OrdinalIgnoreCase))
                {
                    fontRequestCount++;
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            // Construct before suspending so the constructor's own (default Arial) load isn't counted —
            // it uses a different name and so never increments the counter anyway.
            TextRuntime textRuntime = new();

            GraphicalUiElement.IsAllLayoutSuspended = true;
            textRuntime.SetProperty("Font", uniqueFontName);
            textRuntime.SetProperty("FontSize", 23);

            // Deferred: no eager load happened while globally suspended.
            fontRequestCount.ShouldBe(0);
            textRuntime.IsFontDirty.ShouldBeTrue();

            // Resume and flush: the single coalesced load now resolves the font, proving the deferral is
            // realized rather than silently dropped (the #2999 failure mode).
            GraphicalUiElement.IsAllLayoutSuspended = false;
            textRuntime.UpdateFontRecursive();

            fontRequestCount.ShouldBeGreaterThan(0);
            textRuntime.IsFontDirty.ShouldBeFalse();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = savedSuspended;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    #endregion

    #region FontFamily

    [Fact]
    public void Font_ShouldDelegateToFontFamily()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Comic Sans MS";
        sut.Font.ShouldBe("Comic Sans MS");
    }

    [Fact]
    public void FontFamily_ShouldSetAndGetFont()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Impact";
        sut.FontFamily.ShouldBe("Impact");
    }

    #endregion

    #region FontSize

    [Fact]
    public void FontSize_ShouldDefaultTo18()
    {
        TextRuntime sut = new();
        sut.FontSize.ShouldBe(18);
    }

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.FontSize = 24;
        sut.FontSize.ShouldBe(24);
    }

    #endregion

    #region HeightUnits

    [Fact]
    public void HeightUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion

    #region IsBold

    [Fact]
    public void IsBold_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.IsBold.ShouldBeFalse();
    }

    [Fact]
    public void IsBold_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.IsBold = true;
        sut.IsBold.ShouldBeTrue();
    }

    #endregion

    #region IsItalic

    [Fact]
    public void IsItalic_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.IsItalic.ShouldBeFalse();
    }

    [Fact]
    public void IsItalic_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.IsItalic = true;
        sut.IsItalic.ShouldBeTrue();
    }

    #endregion

    #region MaxNumberOfLines

    [Fact]
    public void MaxNumberOfLines_ShouldBeNullByDefault()
    {
        TextRuntime sut = new();
        sut.MaxNumberOfLines.ShouldBeNull();
    }

    [Fact]
    public void MaxNumberOfLines_WhenSetToOne_ShouldLimitWrappedTextToOneLine()
    {
        TextRuntime sut = new();
        sut.Text = "Line1\nLine2\nLine3";
        sut.MaxNumberOfLines = 1;
        sut.WrappedText.Count.ShouldBe(1);
    }

    #endregion

    #region OutlineThickness

    [Fact]
    public void OutlineThickness_ShouldDefaultToZero()
    {
        TextRuntime sut = new();
        sut.OutlineThickness.ShouldBe(0);
    }

    [Fact]
    public void OutlineThickness_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.OutlineThickness = 2;
        sut.OutlineThickness.ShouldBe(2);
    }

    #endregion

    #region TextOverflowHorizontalMode

    [Fact]
    public void TextOverflowHorizontalMode_Default_ShouldBeTruncateWord()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetToEllipsis_ShouldReadBackAsEllipsis()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetBackToTruncate_ShouldReadBackAsTruncate()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.TruncateWord;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    #endregion

    #region WidthUnits

    [Fact]
    public void WidthUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion

    #region SetTextNoTranslate

    [Fact]
    public void SetTextNoTranslate_ShouldUpdateTextProperty()
    {
        TextRuntime sut = new();
        sut.SetTextNoTranslate("Translated Text");
        sut.Text.ShouldBe("Translated Text");
    }

    [Fact]
    public void SetTextNoTranslate_WhenNull_ShouldSetTextToNull()
    {
        TextRuntime sut = new();
        sut.Text = "Some text";
        sut.SetTextNoTranslate(null);
        sut.Text.ShouldBeNull();
    }

    #endregion

    #region UseFontSmoothing

    [Fact]
    public void UseFontSmoothing_ShouldDefaultToTrue()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing.ShouldBeTrue();
    }

    [Fact]
    public void UseFontSmoothing_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing = false;
        sut.UseFontSmoothing.ShouldBeFalse();
    }

    #endregion

    #region WrappedText

    [Fact]
    public void WrappedText_ShouldContainTextLines_AfterTextAssignment()
    {
        TextRuntime sut = new();
        sut.Text = "Line1\nLine2";
        sut.WrappedText.ShouldNotBeEmpty();
    }

    #endregion
}
