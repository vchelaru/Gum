using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// What a Skia <c>Text</c> allocates on Gum's side while BBCode changes every frame (#7846): the parse and the styled
/// runs reuse their lists and run objects, so only the strings the new text needs are built.
/// </summary>
public class TextMarkupChangeAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();

    public TextMarkupChangeAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        _host.Dispose();
        base.Dispose();
    }

    private static string[] BuildStrings()
    {
        string[] strings = new string[1000];
        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = $"[Color=Red]HP[/Color] [IsBold=true]{i}[/IsBold] [FontSize=24]of[/FontSize] [IsItalic=true]999[/IsItalic]";
        }
        return strings;
    }

    // Parsing is all Gum's: the tag list, its sort, the open-tag stack, the variables and their boxed values are reused,
    // so what is left is the strings (tag names and arguments, the stripped text).
    [Fact]
    public void RawText_BbCodeChangedEveryFrame_AllocatesOnlyTheStrings()
    {
        string[] strings = BuildStrings();
        Text text = new Text();
        text.RawText = strings[0];
        int frame = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                frame++;
                text.RawText = strings[frame % strings.Length];
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"RawText set: {result.BytesPerIteration:N0} bytes");
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(1000);
    }

    // The whole frame includes RichTextKit's own allocations, which upstream pull requests are removing, so the limit
    // only guards Gum's share.
    [Fact]
    public void UpdateAndDraw_BbCodeChangedEveryFrame_AllocationBaseline()
    {
        string[] strings = BuildStrings();
        TextRuntime text = new TextRuntime { Text = strings[0] };
        text.AddToRoot();
        int frame = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                frame++;
                text.Text = strings[frame % strings.Length];
                _host.Update();
                _host.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"BBCode changed every frame: {result.BytesPerIteration:N0} bytes/frame");
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(18000);
    }
}
