using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

public interface IReferenceFinder
{
    ElementRenameChanges GetReferencesToElement(ElementSave element, string elementName);

    InstanceRenameChanges GetReferencesToInstance(ElementSave container, InstanceSave instance, string oldName);

    StateRenameChanges GetReferencesToState(StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category);

    CategoryRenameChanges GetReferencesToStateCategory(IStateContainer owner, StateSaveCategory category, string oldName);

    VariableChangeResponse GetReferencesToVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName);
}
