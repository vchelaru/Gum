using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Shouldly;

namespace GumToolUnitTests.ViewModels;

public class AlignmentViewModelTests
{
    [Fact]
    public void NormalizeNegativeZero_ConvertsNegativeZeroToPositiveZero()
    {
        float negativeZero = -0f * 2f;

        negativeZero.ShouldBe(0f);
        float.IsNegative(negativeZero).ShouldBeTrue();

        float normalized = AlignmentViewModel.NormalizeNegativeZero(negativeZero);

        float.IsNegative(normalized).ShouldBeFalse();
    }

    [Fact]
    public void NormalizeNegativeZero_LeavesPositiveZeroAlone()
    {
        float normalized = AlignmentViewModel.NormalizeNegativeZero(0f);

        float.IsNegative(normalized).ShouldBeFalse();
        normalized.ShouldBe(0f);
    }

    [Fact]
    public void NormalizeNegativeZero_LeavesNonZeroValuesUnchanged()
    {
        AlignmentViewModel.NormalizeNegativeZero(-5f).ShouldBe(-5f);
        AlignmentViewModel.NormalizeNegativeZero(5f).ShouldBe(5f);
        AlignmentViewModel.NormalizeNegativeZero(-0.001f).ShouldBe(-0.001f);
    }
}
