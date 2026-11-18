using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// What a warm <c>TextRuntime.Text</c> change allocates on Skia through layout, without drawing (#8237). RichTextKit
/// 0.6.174 reuses its bidi, line break and font run buffers, and <c>GumFontMapper</c> caches typefaces, so Latin text
/// lays out with no managed allocation.
/// </summary>
public class TextChangeLayoutAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public TextChangeLayoutAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string[] BuildStrings(Func<int, string> build)
    {
        string[] strings = new string[1000];
        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = build(i);
        }
        return strings;
    }

    private static AllocationResult MeasureTextChange(string[] strings)
    {
        TextRuntime text = new TextRuntime { Text = strings[0] };
        Text renderable = (Text)text.RenderableComponent;

        // Reading Lines makes RichTextKit lay the block out, which it otherwise does lazily on draw.
        return AllocationMeasurer.MeasureReplayed(
            frame =>
            {
                text.Text = strings[frame % strings.Length];
                _ = renderable.GetCachedTextBlock().Lines.Count;
            },
            warmupIterations: 50,
            measuredIterations: 300);
    }

    [Fact]
    public void Text_LatinChangedEveryFrame_LayoutAllocatesNothing()
    {
        string[] strings = BuildStrings(frame => "Score: " + frame);

        AllocationResult result = MeasureTextChange(strings);

        _output.WriteLine($"Latin text change + layout: {result.BytesPerIteration:N0} bytes/frame");
        result.TotalBytes.ShouldBe(0);
    }
}
