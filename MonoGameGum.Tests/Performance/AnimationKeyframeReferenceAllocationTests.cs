using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using MonoGameGum.Tests.Runtimes;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Resolving the variable references of keyframe states (#7645) must cost nothing for a keyframe state
/// that has none: every playing animation of every element ticks through it.
/// </summary>
public class AnimationKeyframeReferenceAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public AnimationKeyframeReferenceAllocationTests(ITestOutputHelper output)
    {
        _output = output;
        StandardElementsManager.Self.Initialize();
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void ApplyAtTimeTo_KeyframesWithoutReferences_AllocatesNoMoreThanBeforeReferencesWereResolved()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: null, startStoredX: 0f, endRow: null, endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        double time = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                time = (time + 0.05) % 1;
                animation.ApplyAtTimeTo(time, gue);
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"ApplyAtTimeTo without references: {result.BytesPerIteration:N0} bytes/frame");

        gue.X.ShouldBeGreaterThan(0f);
        // 2,088 bytes/frame measured on the code before keyframe references were resolved (the cloned and merged
        // states), 1,168 once the interpolated state is built in objects kept between ticks (#7873).
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(1300);
    }
}
