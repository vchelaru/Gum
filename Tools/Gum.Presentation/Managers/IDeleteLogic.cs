using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System.Collections.Generic;

namespace Gum.Managers;

public interface IDeleteLogic
{
    void HandleDeleteCommand();
    void RemoveElement(ElementSave element);
    void RemoveBehavior(BehaviorSave behavior);
    void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer);
    List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave? componentSave);
    void Remove(StateSave stateSave);
    void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom);
    void RemoveReferencesToInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom);
    void RemoveInstances(List<InstanceSave> instances, ElementSave elementToRemoveFrom);
    void RemoveState(StateSave stateSave, IStateContainer elementToRemoveFrom);
    void DeleteFolders(List<ITreeNode> folderNodes);
}
