using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

public class VariableGridSelectionCoordinatorTests : BaseTestClass
{
    private readonly VariableGridSelectionCoordinator _coordinator = new();

    [Fact]
    public void ShouldRefreshOnInstanceSelected_consumes_pending_state_refresh()
    {
        var instance = new InstanceSave();
        _coordinator.OnStateRefreshed(instance);

        _coordinator.ShouldRefreshOnInstanceSelected(instance).ShouldBeFalse();
        // Second call: pending state refresh has been consumed, must refresh now.
        _coordinator.ShouldRefreshOnInstanceSelected(instance).ShouldBeTrue();
    }

    [Fact]
    public void ShouldRefreshOnInstanceSelected_returns_false_when_state_cascade_was_for_same_instance()
    {
        var instance = new InstanceSave();
        _coordinator.OnStateRefreshed(instance);

        _coordinator.ShouldRefreshOnInstanceSelected(instance).ShouldBeFalse();
    }

    [Fact]
    public void ShouldRefreshOnInstanceSelected_returns_true_when_no_state_cascade()
    {
        var instance = new InstanceSave();

        _coordinator.ShouldRefreshOnInstanceSelected(instance).ShouldBeTrue();
    }

    [Fact]
    public void ShouldRefreshOnInstanceSelected_returns_true_when_state_refresh_was_for_different_instance()
    {
        // Bug repro for issue #2615: user selects instance A, clicks a state in the
        // States pane, then selects instance B. The Variables pane must refresh for B.
        var instanceA = new InstanceSave();
        var instanceB = new InstanceSave();
        _coordinator.OnStateRefreshed(instanceA);

        _coordinator.ShouldRefreshOnInstanceSelected(instanceB).ShouldBeTrue();
    }

    [Fact]
    public void Reset_clears_pending_state_refresh()
    {
        var instance = new InstanceSave();
        _coordinator.OnStateRefreshed(instance);

        _coordinator.Reset();

        _coordinator.ShouldRefreshOnInstanceSelected(instance).ShouldBeTrue();
    }
}
