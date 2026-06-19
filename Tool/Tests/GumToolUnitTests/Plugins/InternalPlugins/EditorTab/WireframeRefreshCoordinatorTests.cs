using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.EditorTab;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class WireframeRefreshCoordinatorTests : BaseTestClass
{
    private readonly WireframeRefreshCoordinator _coordinator = new();

    [Fact]
    public void ShouldRebuildOnElementSelected_consumes_pending_state_rebuild()
    {
        var element = new ScreenSave();
        _coordinator.OnStateRebuild(element);

        _coordinator.ShouldRebuildOnElementSelected(element).ShouldBeFalse();
        // Second call: the pending state rebuild has been consumed, so a later element
        // selection (a genuine new cascade) must rebuild.
        _coordinator.ShouldRebuildOnElementSelected(element).ShouldBeTrue();
    }

    [Fact]
    public void ShouldRebuildOnElementSelected_returns_false_when_state_cascade_was_for_same_element()
    {
        // Selecting an element forces its default state first (HandleStateSelected rebuilds the
        // wireframe), then fires ElementSelected for the same element. The second rebuild is
        // redundant and must be skipped.
        var element = new ScreenSave();
        _coordinator.OnStateRebuild(element);

        _coordinator.ShouldRebuildOnElementSelected(element).ShouldBeFalse();
    }

    [Fact]
    public void ShouldRebuildOnElementSelected_returns_true_when_no_state_cascade()
    {
        var element = new ScreenSave();

        _coordinator.ShouldRebuildOnElementSelected(element).ShouldBeTrue();
    }

    [Fact]
    public void ShouldRebuildOnElementSelected_returns_true_when_state_rebuild_was_for_different_element()
    {
        // Mirrors the variable grid's issue #2615 guard: a state rebuild recorded for element A
        // must not suppress a genuine rebuild when element B is selected.
        var elementA = new ScreenSave();
        var elementB = new ScreenSave();
        _coordinator.OnStateRebuild(elementA);

        _coordinator.ShouldRebuildOnElementSelected(elementB).ShouldBeTrue();
    }

    [Fact]
    public void Reset_clears_pending_state_rebuild()
    {
        var element = new ScreenSave();
        _coordinator.OnStateRebuild(element);

        _coordinator.Reset();

        _coordinator.ShouldRebuildOnElementSelected(element).ShouldBeTrue();
    }
}
