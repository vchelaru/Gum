using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Tests;

public class FontRangesStampTests : IDisposable
{
    private const string TextFnt =
        "info face=\"Arial\" size=18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
        "common lineHeight=21 base=17 scaleW=64 scaleH=64 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
        "page id=0 file=\"Font18Arial_0.png\"\n" +
        "chars count=1\n" +
        "char id=65 x=0 y=0 width=8 height=10 xoffset=0 yoffset=3 xadvance=9 page=0 chnl=15\n" +
        "kernsmith version=\"0.21.0\"\n";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumFontStampTest_" + Guid.NewGuid());

    public FontRangesStampTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string WriteFnt(string contents)
    {
        string path = Path.Combine(_directory, "Font18Arial.fnt");
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void Write_ThenRead_ReturnsTheRanges_AndWritingAgainReplacesTheLine()
    {
        string path = WriteFnt(TextFnt);

        FontRangesStamp.Write(path, "32-126,160-255");
        FontRangesStamp.Write(path, "65-70");

        FontRangesStamp.Read(path).ShouldBe("65-70");
        File.ReadAllLines(path).Count(line => line.StartsWith("gum ")).ShouldBe(1);
    }

    [Fact]
    public void Write_KeepsTheFileLoadableByTheTextParser()
    {
        string path = WriteFnt(TextFnt);

        FontRangesStamp.Write(path, "32-126,160-255");

        ParsedFontFile parsed = new ParsedFontFile(File.ReadAllText(path));
        parsed.Chars.Count.ShouldBe(1);
        parsed.Pages.Select(page => page.File).ShouldBe(new[] { "Font18Arial_0.png" });
    }

    [Fact]
    public void Write_LeavesAFileThatIsNotTheTextFormatAlone()
    {
        const string xmlFnt = "<font><info face=\"Arial\" size=\"18\"/></font>";
        string path = WriteFnt(xmlFnt);

        FontRangesStamp.Write(path, "32-126");

        File.ReadAllText(path).ShouldBe(xmlFnt);
        FontRangesStamp.Read(path).ShouldBeNull();
    }

    [Fact]
    public void Write_DoesNothingWhenTheFileDoesNotExist()
    {
        string path = Path.Combine(_directory, "Missing.fnt");

        FontRangesStamp.Write(path, "32-126");

        File.Exists(path).ShouldBeFalse();
    }

    [Theory]
    [InlineData("32-126,160-255", "160-255,32-126", false)]
    [InlineData("65-70", "65,66,67,68,69,70", false)]
    [InlineData("32-126", "32-127", true)]
    [InlineData("32-126", "32-126,160-255", true)]
    public void IsStale_ComparesTheCodepointsNotTheSpelling(string stamped, string expected, bool shouldBeStale)
    {
        string path = WriteFnt(TextFnt);
        FontRangesStamp.Write(path, stamped);

        FontRangesStamp.IsStale(path, expected, rangesAreFontSpecific: false).ShouldBe(shouldBeStale);
    }

    [Fact]
    public void IsStale_AFontWithNoStampIsCurrentUnlessItsRangesAreFontSpecific_AndNoFileIsNeverStale()
    {
        string path = WriteFnt(TextFnt);

        FontRangesStamp.IsStale(path, "32-126", rangesAreFontSpecific: false).ShouldBeFalse();
        // A per-font entry is newer than any unstamped bake, so that bake can't have used it.
        FontRangesStamp.IsStale(path, "32-126", rangesAreFontSpecific: true).ShouldBeTrue();

        string missing = Path.Combine(_directory, "Missing.fnt");
        FontRangesStamp.IsStale(missing, "32-126", rangesAreFontSpecific: true).ShouldBeFalse();
    }
}
