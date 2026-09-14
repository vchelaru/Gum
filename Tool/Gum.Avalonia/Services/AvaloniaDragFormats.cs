using Avalonia.Input;
using Gum.Managers;

namespace Gum.Avalonia.Services;

/// <summary>
/// The in-process drag formats this head's drag sources and drop targets agree on. Payloads that
/// are not strings (tree nodes) travel in <see cref="Gum.Plugins.InternalPlugins.TreeView.TreeDragPayload"/>;
/// the data transfer carries only a marker.
/// </summary>
public static class AvaloniaDragFormats
{
    /// <summary>A Standards-palette chip: the standard element's type name.</summary>
    public static DataFormat<string> StandardElementName { get; } =
        DataFormat.CreateStringApplicationFormat(DragDropManager.StandardElementNameDataFormat);

    /// <summary>A drag of element-tree nodes; the nodes themselves are in the static payload.</summary>
    public static DataFormat<string> TreeNodes { get; } =
        DataFormat.CreateStringApplicationFormat("Gum.TreeNodes");
}
