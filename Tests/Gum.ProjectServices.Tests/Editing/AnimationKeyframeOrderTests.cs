using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class AnimationKeyframeOrderTests
{
    [Theory]
    [InlineData(-1f, 0)]
    [InlineData(0f, 1)]
    [InlineData(0.5f, 1)]
    [InlineData(1f, 3)]
    [InlineData(2f, 3)]
    public void GetInsertIndex_PutsTheKeyframeAfterEqualTimesAndBeforeLaterOnes(float time, int expected)
    {
        float[] times = { 0f, 1f, 1f };

        AnimationKeyframeOrder.GetInsertIndex(times, item => item, time).ShouldBe(expected);
    }

    [Fact]
    public void GetInsertIndex_OnAnEmptyListIsZero()
    {
        AnimationKeyframeOrder.GetInsertIndex(new List<float>(), item => item, 5f).ShouldBe(0);
    }
}
