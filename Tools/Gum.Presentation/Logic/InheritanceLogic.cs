using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices.Editing;
using System.Collections.Generic;

namespace Gum.Logic;

/// <summary>
/// The tool's reaction to instance and base type changes in elements others inherit from. The data change is
/// <see cref="IInheritanceSync"/>, which gumcli runs too; this adds the saving, pruning and refreshing.
/// </summary>
public class InheritanceLogic
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IBaseTypeVariablePruner _baseTypeVariablePruner;
    private readonly IInheritanceSync _inheritanceSync;

    public InheritanceLogic(IFileCommands fileCommands, IGuiCommands guiCommands,
        IStandardElementsManagerGumTool standardElementsManagerGumTool,
        IBaseTypeVariablePruner baseTypeVariablePruner,
        IInheritanceSync inheritanceSync)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _baseTypeVariablePruner = baseTypeVariablePruner;
        _inheritanceSync = inheritanceSync;
    }

    public void HandleInstanceAdded(ElementSave container, InstanceSave instance) =>
        SaveAndRefresh(_inheritanceSync.AddInstance(container, instance));

    public void HandleInstanceDeleted(ElementSave? container, InstanceSave instance)
    {
        if (container == null) return;

        SaveAndRefresh(_inheritanceSync.RemoveInstance(container, instance));
    }

    public void HandleInstanceRenamed(ElementSave? container, InstanceSave instance, string oldName)
    {
        if (container == null) return;

        SaveAndRefresh(_inheritanceSync.RenameInstance(container, instance, oldName));
    }

    public void HandleInstanceReordered(InstanceSave instance) =>
        SaveAndRefresh(_inheritanceSync.ReorderInstance(instance));

    /// <summary>
    /// Reacts to the instance's base type having changed: removes the variables the new type cannot have from the
    /// container (see <see cref="IBaseTypeVariablePruner"/>; undo restores them), then follows the type in the
    /// elements inheriting from the container. The caller holds the undo lock of the edit.
    /// </summary>
    public void HandleInstanceVariableBaseTypeSet(ElementSave container, InstanceSave instance)
    {
        _baseTypeVariablePruner.PruneInstance(container, instance);

        foreach (ElementSave inheritingElement in _inheritanceSync.SetInstanceBaseType(container, instance))
        {
            _fileCommands.TryAutoSaveElement(inheritingElement);
        }
    }

    /// <summary>
    /// Reacts to the element's base type having changed: removes the variables the new type cannot have (see
    /// <see cref="IBaseTypeVariablePruner"/>; undo restores them) and replaces the instances inherited from the old
    /// base. The caller holds the undo lock of the edit.
    /// </summary>
    public void HandleElementBaseType(ElementSave asElementSave)
    {
        _baseTypeVariablePruner.PruneElement(asElementSave);

        _inheritanceSync.ReplaceInheritedInstances(asElementSave);
        _standardElementsManagerGumTool.FixCustomTypeConverters(asElementSave);

        const bool fullRefresh = true;
        _guiCommands.RefreshElementTreeView(asElementSave);
        _guiCommands.RefreshVariables(fullRefresh);
        _guiCommands.RefreshStateTreeView();
    }

    private void SaveAndRefresh(IReadOnlyList<ElementSave> changedElements)
    {
        foreach (ElementSave element in changedElements)
        {
            _fileCommands.TryAutoSaveElement(element);
            _guiCommands.RefreshElementTreeView(element);
        }
    }
}
