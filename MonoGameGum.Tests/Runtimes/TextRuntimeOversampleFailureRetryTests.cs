using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #8035: a font the creator cannot build at the oversampled size must not be retried (and its
// error re-reported) every frame, for every zoomed Text.
public class TextRuntimeOversampleFailureRetryTests : BaseTestClass
{
    private readonly bool _savedUseFontOversampling = TextRuntime.UseFontOversampling;
    private readonly IInMemoryFontCreator? _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
    private readonly List<TextRuntime> _texts = new();

    public TextRuntimeOversampleFailureRetryTests()
    {
        TextRuntime.UseFontOversampling = true;
    }

    public override void Dispose()
    {
        // Shared oversampled fonts are static; turning oversampling off puts each text back on its native font.
        TextRuntime.UseFontOversampling = false;
        foreach (TextRuntime textRuntime in _texts)
        {
            textRuntime.UpdateAutomaticFontOversampling(1f);
        }
        TextRuntime.UseFontOversampling = _savedUseFontOversampling;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        base.Dispose();
    }

    private TextRuntime CreateText(string font = "FailFont")
    {
        // The creator also builds a Text's native font, so keep it out of the call counts.
        IInMemoryFontCreator? creator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        TextRuntime textRuntime = new();
        textRuntime.Font = font;
        textRuntime.FontSize = 18;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        _texts.Add(textRuntime);
        return textRuntime;
    }

    private static Text GetText(TextRuntime textRuntime) => (Text)textRuntime.RenderableComponent!;

    private static void Frames(TextRuntime textRuntime, float zoom, int count)
    {
        for (int i = 0; i < count; i++)
        {
            textRuntime.UpdateAutomaticFontOversampling(zoom);
        }
    }

    private static BitmapFont CreateStubFont()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, StubFontData);
        font.SetFontPattern(256, 256);
        return font;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateAutomaticFontOversampling_CreatorFailsAtUnchangedZoom_IsNotRetriedEveryFrame(bool creatorThrows)
    {
        ScriptedCreator creator = new() { Throw = creatorThrows };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime textRuntime = CreateText();

        Frames(textRuntime, 2f, 5);

        creator.Calls.ShouldBe(1);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_CreatorThrowsEveryFrame_ReportsTheErrorOnce()
    {
        ScriptedCreator creator = new() { Throw = true };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime textRuntime = CreateText();
        int errors = 0;
        Action<string> onError = _ => errors++;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += onError;
        try
        {
            Frames(textRuntime, 2f, 5);
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= onError;
        }

        errors.ShouldBe(1);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_CreatorFails_KeepsTheNativeFont()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new ScriptedCreator { Throw = true };
        TextRuntime textRuntime = CreateText();
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;

        Frames(textRuntime, 2f, 3);

        text.BitmapFont.ShouldBeSameAs(nativeFont);
        text.OversampleCompensationScale.ShouldBe(1f);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_ManyTextsShareTheFailingFont_MakeOneAttempt()
    {
        ScriptedCreator creator = new() { Throw = true };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        List<TextRuntime> texts = new();
        for (int i = 0; i < 10; i++)
        {
            texts.Add(CreateText());
        }

        for (int frame = 0; frame < 5; frame++)
        {
            foreach (TextRuntime textRuntime in texts)
            {
                textRuntime.UpdateAutomaticFontOversampling(2f);
            }
        }

        creator.Calls.ShouldBe(1);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_TwoDifferentFailingFonts_EachGetsOneAttempt()
    {
        ScriptedCreator creator = new() { Throw = true };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText("FailFontA");
        TextRuntime second = CreateText("FailFontB");

        Frames(first, 2f, 4);
        Frames(second, 2f, 4);

        creator.Calls.ShouldBe(2);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_FailedFamily_IsNotRetriedAtAnySize()
    {
        ScriptedCreator creator = new() { Throw = true };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime textRuntime = CreateText();

        Frames(textRuntime, 2f, 3);
        Frames(textRuntime, 3f, 3);
        Frames(textRuntime, 1.5f, 3);
        Frames(textRuntime, 2f, 3);

        creator.Calls.ShouldBe(1);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_FailedFamily_StillRendersAtItsRegularSize()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new ScriptedCreator { Throw = true };
        TextRuntime textRuntime = CreateText();
        textRuntime.Text = "Hello";
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;
        float widthBefore = text.WrappedTextWidth;

        Should.NotThrow(() => Frames(textRuntime, 2f, 5));

        nativeFont.ShouldNotBeNull();
        text.BitmapFont.ShouldBeSameAs(nativeFont);
        text.OversampleCompensationScale.ShouldBe(1f);
        text.WrappedTextWidth.ShouldBe(widthBefore);
        textRuntime.Visible.ShouldBeTrue();
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_SameCreatorAssignedAgainAfterAFailure_TriesAgain()
    {
        ScriptedCreator creator = new() { Throw = true };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime textRuntime = CreateText();
        Frames(textRuntime, 2f, 3);

        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        Frames(textRuntime, 2f, 3);

        creator.Calls.ShouldBe(2);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_CreatorReplacedAfterAFailure_TriesTheNewCreatorImmediately()
    {
        BitmapFont workingFont = CreateStubFont();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new ScriptedCreator { Throw = true };
        TextRuntime textRuntime = CreateText();
        Frames(textRuntime, 2f, 3);
        ScriptedCreator replacement = new() { FontForRasterSize = _ => workingFont };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = replacement;

        Frames(textRuntime, 2f, 1);

        GetText(textRuntime).BitmapFont.ShouldBeSameAs(workingFont);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_AnotherTextAlreadySharesTheFont_UsesItWithoutTheCreator()
    {
        BitmapFont workingFont = CreateStubFont();
        ScriptedCreator creator = new() { FontForRasterSize = _ => workingFont };
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime first = CreateText("SharedOkFont");
        TextRuntime second = CreateText("SharedOkFont");

        Frames(first, 2f, 3);
        Frames(second, 2f, 3);

        creator.Calls.ShouldBe(1);
        GetText(second).BitmapFont.ShouldBeSameAs(workingFont);
    }

    [Fact]
    public void UpdateAutomaticFontOversampling_NoCreatorAndNoFontCache_KeepsTheNativeFont()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        TextRuntime textRuntime = CreateText();
        Text text = GetText(textRuntime);
        BitmapFont? nativeFont = text.BitmapFont;

        Should.NotThrow(() => Frames(textRuntime, 2f, 5));

        text.BitmapFont.ShouldBeSameAs(nativeFont);
    }

    private sealed class ScriptedCreator : IInMemoryFontCreator
    {
        public bool Throw;
        public int Calls;
        public Func<float, BitmapFont?>? FontForRasterSize;

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            Calls++;
            BitmapFont? font = FontForRasterSize?.Invoke(bmfcSave.FontSize);
            if (font != null)
            {
                return font;
            }
            if (Throw)
            {
                throw new InvalidOperationException($"System font '{bmfcSave.FontName}' not found");
            }
            return null;
        }
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""StubFont_0.png""
chars count=1
char id=65   x=0   y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";
}
