using System.Linq;
using Gum.DataTypes;
using Gum.Managers;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Decides whether an instance under a file dropped on the wireframe can take that file.
/// </summary>
internal interface IFileDropTargetFilter
{
    /// <summary>
    /// Whether <paramref name="instance"/>'s root standard type has a SourceFile variable.
    /// </summary>
    bool CanReceiveSourceFile(InstanceSave instance);

    /// <summary>
    /// Whether <paramref name="instance"/>'s root standard type is Text.
    /// </summary>
    bool CanReceiveFont(InstanceSave instance);
}

/// <inheritdoc cref="IFileDropTargetFilter"/>
internal class FileDropTargetFilter : IFileDropTargetFilter
{
    /// <inheritdoc/>
    public bool CanReceiveSourceFile(InstanceSave instance)
    {
        // Null when the base type chain is broken, e.g. a component whose base type was deleted.
        StandardElementSave? baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(instance);

        return baseStandardElement?.DefaultState?.Variables.Any(v => v.Name == "SourceFile") == true;
    }

    /// <inheritdoc/>
    public bool CanReceiveFont(InstanceSave instance)
    {
        StandardElementSave? baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(instance);

        return baseStandardElement?.Name == "Text";
    }
}
