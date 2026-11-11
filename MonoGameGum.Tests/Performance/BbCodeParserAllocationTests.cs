using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// The parser returns the known set's own tag-name string instead of copying each name out of the text, so a parse
/// allocates only the tag arguments (which have to be strings) and the result entries.
/// </summary>
public class BbCodeParserAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public BbCodeParserAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_KnownTagsInAnyCasing_AllocatesNoTagNameStrings()
    {
        // Six tags (twelve names counting closes) plus one unknown tag.
        const string text =
            "[COLOR=Red]a[/color] [isbold=true]b[/IsBold] [FontSize=24]c[/FontSize] [Alpha=5]d[/Alpha] " +
            "[IsItalic=true]e[/IsItalic] [Link=x]f[/Link] [Zork]g";

        List<FoundTag> results = new List<FoundTag>();
        List<BbCodeParser.Tag> activeTags = new List<BbCodeParser.Tag>();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                results.Clear();
                activeTags.Clear();
                BbCodeParser.Parse(text, BbCodeParser.KnownTags, results, activeTags);
            },
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"Parse: {result.BytesPerIteration:N0} bytes");
        results.Count.ShouldBe(6);
        // 640 bytes before, 176 measured after on net10.0 (the only target MonoGameGum.Tests has); what is left is the six
        // arguments (Red, true, 24, 5, true, x), which must be strings. 224 leaves 48 bytes for runtime string-size changes;
        // copying the twelve tag names again would be about 460 bytes more and fail.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(224);
    }
}
