using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4542: TextRuntime.UseAutomaticFontGrowth (opt-in, mirrors UseFontOversampling's shape) makes a
// Text notice a character missing from its live BitmapFont and grow the font in place -- fully
// automatic, no explicit call needed from the game. Detection/growth must be synchronous inside the
// Text assignment itself (not deferred to next-frame PreRender the way oversampling regeneration is),
// since e.g. a TextBox keystroke measures wrap immediately, before the property setter returns.
public class TextRuntimeAutomaticFontGrowthTests : BaseTestClass
{
    [Fact]
    public void Text_WhenAutomaticFontGrowthDisabled_MissingCharacterStaysUngrown()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = false;
            GrowableStubFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";
            textRuntime.Text = "C";

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter('C').ShouldBeFalse();
            creator.TryAddGlyphsCalls.ShouldBeEmpty();
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenAutomaticFontGrowthEnabled_MissingCharacterIsGrownSynchronously()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            GrowableStubFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";
            textRuntime.Text = "C";

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter('C').ShouldBeTrue(
                "because the missing character must be grown synchronously, in the same Text assignment that discovered it");
            creator.TryAddGlyphsCalls.ShouldContain("C");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenAutomaticFontGrowthEnabled_AllCharactersAlreadyPresent_NeverCallsGrowth()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            GrowableStubFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";
            // Isolate from the constructor's own default "Hello" text, which the stub font (only
            // knowing space/'A'/'B') would itself have already triggered growth calls for.
            creator.TryAddGlyphsCalls.Clear();
            textRuntime.Text = "AB BA"; // 'A'/'B'/space are all in GrowableStubFontCreator's base charset

            creator.TryAddGlyphsCalls.ShouldBeEmpty(
                "because a font that already has every requested character must never be asked to grow");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenACharacterCannotBeRendered_RaisesPropertyAssignmentError_AndDoesNotGrowIt()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        List<string> reportedMessages = new();
        System.Action<string> handler = reportedMessages.Add;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            GrowableStubFontCreator creator = new();
            creator.UnrenderableCharacters.Add('Z');
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";
            reportedMessages.Clear(); // isolate from whatever the Font/constructor cascade already reported
            textRuntime.Text = "Z";

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter('Z').ShouldBeFalse();
            reportedMessages.ShouldHaveSingleItem();
            reportedMessages[0].ShouldContain("Z");
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenInMemoryFontCreatorDoesNotSupportGrowth_DoesNothingSilently()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            // Implements IInMemoryFontCreator only -- no IGrowableFontCreator, same as any existing
            // custom creator written before this feature existed.
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NonGrowableStubFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";

            Should.NotThrow(() => textRuntime.Text = "C");
            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter('C').ShouldBeFalse();
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4542 design decision #3: oversampling keeps two live fonts (pinned MeasurementFont,
    // regenerated display font). A full RegenerateOversampledFont builds a brand-new BitmapFont from
    // BmfcSave.Ranges alone -- it has no idea about characters grown in at runtime, so continuous
    // zooming would silently drop them on every regenerate unless growth history is replayed into
    // each freshly-generated font.
    [Fact]
    public void RegenerateOversampledFont_ReplaysPreviouslyGrownCharacters_IntoTheFreshOversampledFont()
    {
        bool savedOversampling = TextRuntime.UseFontOversampling;
        bool savedGrowth = TextRuntime.UseAutomaticFontGrowth;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            TextRuntime.UseAutomaticFontGrowth = true;
            GrowableStubFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = "StubFont";
            textRuntime.FontSize = 20;
            textRuntime.Text = "C"; // grows the native/measurement font in place

            Text text = (Text)textRuntime.RenderableComponent;
            text.BitmapFont.HasCharacter('C').ShouldBeTrue("because the native font must have grown first");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            // A brand-new BitmapFont instance (GrowableStubFontCreator.TryCreateFont always returns a
            // fresh one) built from the base charset alone would NOT have 'C' unless growth history
            // was replayed into it.
            text.BitmapFont.HasCharacter('C').ShouldBeTrue(
                "because previously-grown characters must be replayed into every freshly-regenerated oversampled font");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedOversampling;
            TextRuntime.UseAutomaticFontGrowth = savedGrowth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private const string BaseFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""StubFont_0.png""
chars count=3
char id=32   x=0   y=0   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=65   x=0   y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
char id=66   x=10  y=0   width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";

    // Only knows space/'A'/'B' at creation time -- any other character is "missing" and a candidate
    // for growth. Returns a fresh BitmapFont instance every call (unlike the identical-stub fakes used
    // by the oversampling tests) so the replay test above can tell "brand-new font" from "same object,
    // still has what it had before".
    private sealed class GrowableStubFontCreator : IInMemoryFontCreator, IGrowableFontCreator
    {
        public List<string> TryAddGlyphsCalls { get; } = new();
        public HashSet<char> UnrenderableCharacters { get; } = new();

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, BaseFontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        public IReadOnlyList<char>? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
        {
            TryAddGlyphsCalls.Add(characters);
            List<char> failed = new();
            foreach (char c in characters)
            {
                if (font.HasCharacter(c))
                {
                    continue;
                }
                if (UnrenderableCharacters.Contains(c))
                {
                    failed.Add(c);
                    continue;
                }
                font.AddOrUpdateCharacter(
                    new FontFileCharLine { Id = c, Width = 10, Height = 10, XAdvance = 10 }, 256, 256);
            }
            return failed;
        }
    }

    private sealed class NonGrowableStubFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, BaseFontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }
}
