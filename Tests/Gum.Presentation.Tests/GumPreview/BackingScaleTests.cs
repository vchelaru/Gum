using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class BackingScaleTests
{
    [Fact]
    public void ToPoints_Retina_HalvesAndRoundsUpSoTheCanvasFits()
    {
        BackingScale scale = new BackingScale(2);

        (int width, int height) = scale.ToPoints(800, 601);

        width.ShouldBe(400);
        height.ShouldBe(301);
    }

    [Fact]
    public void ToPixels_Retina_Doubles()
    {
        BackingScale scale = new BackingScale(2);

        (int width, int height) = scale.ToPixels(400, 301);

        width.ShouldBe(800);
        height.ShouldBe(602);
    }

    [Fact]
    public void IsScaled_StandardDisplay_IsFalseAndLeavesSizesAlone()
    {
        BackingScale scale = new BackingScale(1);

        scale.IsScaled.ShouldBeFalse();
        scale.ToPoints(800, 600).ShouldBe((800, 600));
    }
}
