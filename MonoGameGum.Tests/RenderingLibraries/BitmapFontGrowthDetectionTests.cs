using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// Issue #4542: TextRuntime auto-growth needs a cheap way to ask "does this font already have every
// character in this string" as a one-shot pre-pass -- not per-character during the hot wrap loop
// (GetCharacterInfo already silently falls back to the space glyph for anything missing, which is
// exactly the ambiguity growth detection needs to see through).
public class BitmapFontGrowthDetectionTests
{
    private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=3
char id=32   x=206   y=102   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=65   x=0     y=0     width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
char id=66   x=10    y=0     width=10    height=10    xoffset=0     yoffset=0     xadvance=10    page=0  chnl=15
";

    private static BitmapFont NewFont()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
        font.SetFontPattern(256, 256);
        return font;
    }

    [Fact]
    public void HasCharacter_ReturnsTrue_ForCharacterParsedFromFntFile()
    {
        NewFont().HasCharacter('A').ShouldBeTrue();
    }

    [Fact]
    public void HasCharacter_ReturnsFalse_ForCharacterNotInFntFile()
    {
        NewFont().HasCharacter('Z').ShouldBeFalse();
    }

    [Theory]
    [InlineData('\t')]
    [InlineData('\n')]
    public void HasCharacter_ReturnsTrue_ForStructuralWhitespace_EvenThoughNeverInFntChars(char structuralChar)
    {
        // '\t'/'\n' are synthesized by SetFontPattern from the space glyph, never listed as their own
        // <char> entry -- they must still read as "present" so growth never tries (and fails) to add them.
        NewFont().HasCharacter(structuralChar).ShouldBeTrue();
    }

    [Fact]
    public void HasCharacter_ReturnsFalse_ForCodepointBeyondTheParsedCharacterArray()
    {
        NewFont().HasCharacter((char)9999).ShouldBeFalse();
    }

    [Fact]
    public void AddOrUpdateCharacter_MarksTheNewCharacterAsPresent()
    {
        BitmapFont font = NewFont();
        font.HasCharacter('C').ShouldBeFalse();

        font.AddOrUpdateCharacter(new FontFileCharLine { Id = 'C', Width = 10, Height = 10, XAdvance = 10 }, 256, 256);

        font.HasCharacter('C').ShouldBeTrue();
    }

    [Fact]
    public void GetMissingCharacters_ReturnsEmptyString_WhenEveryCharacterIsPresent()
    {
        NewFont().GetMissingCharacters("AB BA").ShouldBe(string.Empty);
    }

    [Fact]
    public void GetMissingCharacters_ReturnsOnlyTheDistinctMissingCharacters_InFirstEncounterOrder()
    {
        // 'Z' and 'Y' are both missing; 'Z' repeats. 'A'/space are present and must be excluded.
        string missing = NewFont().GetMissingCharacters("AZ ZY");

        missing.ShouldBe("ZY");
    }

    [Fact]
    public void GetMissingCharacters_ReturnsEmptyString_ForNullOrEmptyInput()
    {
        BitmapFont font = NewFont();

        font.GetMissingCharacters(null).ShouldBe(string.Empty);
        font.GetMissingCharacters(string.Empty).ShouldBe(string.Empty);
    }
}
