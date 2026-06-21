using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System.Collections.Generic;

namespace Gum.Logic;

public interface ICopyPasteLogic
{
    CopiedData CopiedData { get; }
    void ForceSelectionChanged();
    void OnCopy(CopyType copyType);
    void OnCut(CopyType copyType);
    void OnPaste(CopyType copyType, TopOrRecursive topOrRecursive = TopOrRecursive.Recursive);
    List<InstanceSave> PasteInstanceSaves(List<InstanceSave> instancesToCopy,
        List<StateSave> copiedStates,
        ElementSave targetElement,
        InstanceSave? selectedInstance,
        ISelectedState? forcedSelectedState = null,
        List<StateSave>? baseElementDefaultStates = null,
        HashSet<string>? itemsOwnedByReachableStates = null,
        List<InstanceSave>? instancesToSelectAfterPaste = null);

    /// <summary>
    /// Promotes a single instance into a brand-new component: the instance's type becomes the
    /// component's base type, the instance's children become the component's instances, and the
    /// instance's intrinsic variables (everything except parent-relative position) become the
    /// component's root variables. Optionally replaces the original instance subtree with a single
    /// instance of the new component, preserving the original instance's position.
    /// </summary>
    /// <remarks>
    /// This is the data-mutation core behind the "Create Component" tree-view command; user-facing
    /// callers should go through <see cref="Commands.IEditCommands.ShowCreateComponentFromInstancesDialog"/>,
    /// which gathers the name and replace option before delegating here.
    /// </remarks>
    /// <param name="instance">The instance to promote. Its <see cref="InstanceSave.ParentContainer"/> is the source element.</param>
    /// <param name="componentName">The name (optionally folder-qualified) of the new component.</param>
    /// <param name="replaceWithInstance">When true, runs Phase 2: the original subtree is removed and replaced with one instance of the new component.</param>
    /// <returns>The newly created and project-registered component.</returns>
    ComponentSave CreateComponentFromInstance(InstanceSave instance, string componentName, bool replaceWithInstance);
}
