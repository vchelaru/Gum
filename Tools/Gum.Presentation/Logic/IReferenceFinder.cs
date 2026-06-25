using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

public interface IReferenceFinder
{
    BehaviorReferences GetReferencesToBehavior(BehaviorSave behavior, string oldName);

    ElementReferences GetReferencesToElement(ElementSave element, string elementName);

    InstanceReferences GetReferencesToInstance(ElementSave container, InstanceSave instance, string oldName);

    StateReferences GetReferencesToState(StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category);

    CategoryReferences GetReferencesToStateCategory(IStateContainer owner, StateSaveCategory category, string oldName);

    VariableChangeResponse GetReferencesToVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName);
}
