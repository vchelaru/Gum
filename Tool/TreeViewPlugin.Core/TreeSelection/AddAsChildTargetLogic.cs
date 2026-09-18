using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Picks the tree node a Ctrl+Shift-click add goes under: the container the last add used, when
/// <see cref="IAddDestinationTracker"/> still remembers it and it still has a node, else the
/// selected node (#4846). Keeps repeated adds as siblings even though each add selects its new
/// child.
/// </summary>
public class AddAsChildTargetLogic
{
    private readonly IAddDestinationTracker _addDestinationTracker;

    public AddAsChildTargetLogic(IAddDestinationTracker addDestinationTracker)
    {
        _addDestinationTracker = addDestinationTracker;
    }

    /// <summary>
    /// Returns the node to add under, or null when nothing is remembered and nothing is selected.
    /// </summary>
    public ITreeNode? GetTarget(IElementTreeRoots roots, ITreeNode? selectedNode)
    {
        ITreeNode? remembered = _addDestinationTracker.Destination switch
        {
            InstanceSave instance => roots.GetTreeNodeFor(instance.ParentContainer)?.GetTreeNodeFor(instance),
            ElementSave element => roots.GetTreeNodeFor(element),
            _ => null
        };

        return remembered ?? selectedNode;
    }
}
