using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Setting a color or one of its channels reports every channel to the variable references that read it
/// (#6410). That report must not build the channel names per call (#8218).
/// </summary>
public class ColorChannelAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ColorChannelAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("FillAlpha")]
    [InlineData("StrokeAlpha")]
    [InlineData("DropshadowAlpha")]
    public void SetChannel_Warm_AllocatesNothing(string channel)
    {
        RectangleRuntime sut = new();
        Action<int> set = channel switch
        {
            "FillAlpha" => value => sut.FillAlpha = value,
            "StrokeAlpha" => value => sut.StrokeAlpha = value,
            _ => value => sut.DropshadowAlpha = value,
        };
        int value = 0;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                value = (value + 1) % 256;
                set(value);
            },
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"{channel}: {result.BytesPerIteration:N0} bytes/set");
        result.BytesPerIteration.ShouldBe(0);
    }
}
