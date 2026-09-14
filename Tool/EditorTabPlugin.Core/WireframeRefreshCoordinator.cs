using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.EditorTab;

/// <summary>
/// Coordinates the synchronous selection cascade between the state and element events for the
/// wireframe. Selecting an element from the tree forces its default state first, so the cascade
/// fires the state event (which rebuilds the wireframe) and then the element event for the same
/// element. The element event's rebuild is therefore redundant and can be skipped — but only when
/// the cascade was for the same element that the state rebuild just ran for. A standalone state
/// rebuild for element A must not suppress a genuine rebuild when element B is later selected
/// (mirrors the variable grid's issue #2615 guard via <see cref="VariableGrid.VariableGridSelectionCoordinator"/>).
/// </summary>
internal class WireframeRefreshCoordinator
{
    private bool _hasPendingStateRebuild;
    private ElementSave? _elementFromStateRebuild;

    /// <summary>
    /// Records that the state event just rebuilt the wireframe for <paramref name="currentElement"/>.
    /// Call this after the state-driven rebuild so the element event that follows in the same
    /// cascade can skip its redundant rebuild.
    /// </summary>
    public void OnStateRebuild(ElementSave? currentElement)
    {
        _hasPendingStateRebuild = true;
        _elementFromStateRebuild = currentElement;
    }

    /// <summary>
    /// Returns <c>false</c> when the immediately-preceding state rebuild in this cascade already
    /// rebuilt the wireframe for <paramref name="newElement"/>, meaning the element event should
    /// skip its rebuild. Consumes the pending state rebuild so it only suppresses one element event.
    /// </summary>
    public bool ShouldRebuildOnElementSelected(ElementSave? newElement)
    {
        var skip = _hasPendingStateRebuild && newElement == _elementFromStateRebuild;
        _hasPendingStateRebuild = false;
        _elementFromStateRebuild = null;
        return !skip;
    }

    public void Reset()
    {
        _hasPendingStateRebuild = false;
        _elementFromStateRebuild = null;
    }
}
