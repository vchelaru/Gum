using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class BBCodeParserTests : BaseTestClass
{
    [Fact]
    public void Parse_ShouldParseOverlappingColors()
    {
        HashSet<string> tags = new() { "Color" };
        List<FoundTag> parsedTags = BbCodeParser.Parse(
            "[Color=Blue]1[Color=Red]2[/Color]3[/Color]", 
            tags);

        parsedTags.Count.ShouldBe(2);
        parsedTags[0].Open.StartStrippedIndex.ShouldBe(0);
        parsedTags[0].Close.StartStrippedIndex.ShouldBe(3);

        parsedTags[1].Open.StartStrippedIndex.ShouldBe(1);
        parsedTags[1].Close.StartStrippedIndex.ShouldBe(2);
    }
}
