using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

// NineSliceSourceCut is where every runtime's nine-slice cuts its texture (#8354).
public class NineSliceSourceCutTests
{
    [Fact]
    public void Compute_NothingSet_ShouldCutAThirdOfEachAxis()
    {
        NineSliceSourceCut sut = NineSliceSourceCut.Compute(12, 16, null, null, null, null, null);

        sut.Left.ShouldBe(4);
        sut.Right.ShouldBe(4);
        sut.Top.ShouldBe(5);
        sut.Bottom.ShouldBe(5);
    }

    [Fact]
    public void Compute_FrameWidthOnly_ShouldCutEverySideAtIt()
    {
        NineSliceSourceCut sut = NineSliceSourceCut.Compute(12, 16, 3f, null, null, null, null);

        sut.Left.ShouldBe(3);
        sut.Top.ShouldBe(3);
        sut.Right.ShouldBe(3);
        sut.Bottom.ShouldBe(3);
    }

    [Fact]
    public void Compute_PerSideValues_ShouldOverrideFrameWidthAndTheThird()
    {
        NineSliceSourceCut withFrame = NineSliceSourceCut.Compute(12, 16, 1f, 6f, 8f, 2f, 3f);
        NineSliceSourceCut leftOnlyWithFrame = NineSliceSourceCut.Compute(12, 16, 1f, 6f, null, null, null);
        NineSliceSourceCut topOnlyWithoutFrame = NineSliceSourceCut.Compute(12, 16, null, null, 8f, null, null);

        withFrame.Left.ShouldBe(6);
        withFrame.Top.ShouldBe(8);
        withFrame.Right.ShouldBe(2);
        withFrame.Bottom.ShouldBe(3);

        leftOnlyWithFrame.Left.ShouldBe(6);
        leftOnlyWithFrame.Top.ShouldBe(1);
        leftOnlyWithFrame.Right.ShouldBe(1);
        leftOnlyWithFrame.Bottom.ShouldBe(1);

        topOnlyWithoutFrame.Top.ShouldBe(8);
        topOnlyWithoutFrame.Left.ShouldBe(4);
        topOnlyWithoutFrame.Bottom.ShouldBe(5);
    }

    [Fact]
    public void Compute_NegativeOrFractional_ShouldRoundAndClampToZero()
    {
        NineSliceSourceCut sut = NineSliceSourceCut.Compute(12, 16, -2f, 2.5f, null, null, null);

        sut.Left.ShouldBe(3);
        sut.Top.ShouldBe(0);
    }
}
