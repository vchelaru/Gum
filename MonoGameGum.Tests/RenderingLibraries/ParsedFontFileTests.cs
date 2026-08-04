using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class ParsedFontFileTests
{
    const string xmlFontWithEverySection = @"<?xml version=""1.0""?>
<font>
  <info face=""Arial"" size=""-18"" bold=""0"" italic=""0"" charset="""" unicode=""1"" stretchH=""100"" smooth=""1"" aa=""1"" padding=""0,0,0,0"" spacing=""1,1"" outline=""2""/>
  <common lineHeight=""21"" base=""17"" scaleW=""256"" scaleH=""256"" pages=""2"" packed=""0"" alphaChnl=""0"" redChnl=""4"" greenChnl=""4"" blueChnl=""4""/>
  <pages>
    <page id=""0"" file=""Font18Arial_0.png""/>
    <page id=""1"" file=""Font18Arial_1.png""/>
  </pages>
  <chars count=""1"">
    <char id=""65"" x=""206"" y=""102"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""20"" xadvance=""5"" page=""1"" chnl=""15""/>
  </chars>
  <kernings count=""1"">
    <kerning first=""65"" second=""86"" amount=""-2""/>
  </kernings>
</font>";

    const string xmlFontWithoutOutline = @"<?xml version=""1.0""?>
<font>
  <info face=""Arial"" size=""-18""/>
  <common lineHeight=""21"" base=""17""/>
  <pages>
    <page id=""0"" file=""Font18Arial_0.png""/>
  </pages>
  <chars count=""1"">
    <char id=""65"" x=""206"" y=""102"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""20"" xadvance=""5"" page=""0"" chnl=""15""/>
  </chars>
</font>";

    [Fact]
    public void Constructor_ShouldDefaultOmittedAttributesAndSections_InXmlFormat()
    {
        ParsedFontFile parsedFontFile = new ParsedFontFile(xmlFontWithoutOutline);

        parsedFontFile.Info.Outline.ShouldBe(0);
        parsedFontFile.Kernings.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldParseEverySectionOfXmlFormat()
    {
        ParsedFontFile parsedFontFile = new ParsedFontFile(xmlFontWithEverySection);

        parsedFontFile.Info.Size.ShouldBe(18, "because a negative size in the file means the size was matched to character height");
        parsedFontFile.Info.Outline.ShouldBe(2);

        parsedFontFile.Common.LineHeight.ShouldBe(21);
        parsedFontFile.Common.Base.ShouldBe(17);

        parsedFontFile.Pages.Count.ShouldBe(2);
        parsedFontFile.Pages[1].Id.ShouldBe(1);
        parsedFontFile.Pages[1].File.ShouldBe("Font18Arial_1.png");
        parsedFontFile.GetPagesAsArrayOfStrings.ShouldBe(new[] { "Font18Arial_0.png", "Font18Arial_1.png" });

        FontFileCharLine charLine = parsedFontFile.Chars.ShouldHaveSingleItem();
        charLine.Id.ShouldBe(65);
        charLine.X.ShouldBe(206);
        charLine.Y.ShouldBe(102);
        charLine.Width.ShouldBe(3);
        charLine.Height.ShouldBe(1);
        charLine.XOffset.ShouldBe(-1);
        charLine.YOffset.ShouldBe(20);
        charLine.XAdvance.ShouldBe(5);
        charLine.Page.ShouldBe(1);

        FontFileKerningLine kerningLine = parsedFontFile.Kernings.ShouldHaveSingleItem();
        kerningLine.First.ShouldBe(65);
        kerningLine.Second.ShouldBe(86);
        kerningLine.Amount.ShouldBe(-2);
    }
}
