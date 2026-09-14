using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Coordinates the synchronous selection cascade between state and instance events
/// for the Variables grid. When a new instance is selected from the tree, the cascade
/// fires the state event first (forcing the default state) and then the instance event.
/// The state event has already done a full grid refresh, so the instance event can skip
/// its refresh — but only when the cascade was for the same instance that just got
/// selected. A standalone state-pane click followed later by a different instance
/// selection must still refresh (issue #2615).
/// </summary>
internal class VariableGridSelectionCoordinator
{
    private bool _hasPendingStateRefresh;
    private InstanceSave? _instanceFromStateRefresh;

    public void OnStateRefreshed(InstanceSave? currentInstance)
    {
        _hasPendingStateRefresh = true;
        _instanceFromStateRefresh = currentInstance;
    }

    public bool ShouldRefreshOnInstanceSelected(InstanceSave? newInstance)
    {
        var skip = _hasPendingStateRefresh && newInstance == _instanceFromStateRefresh;
        _hasPendingStateRefresh = false;
        _instanceFromStateRefresh = null;
        return !skip;
    }

    public void Reset()
    {
        _hasPendingStateRefresh = false;
        _instanceFromStateRefresh = null;
    }
}
