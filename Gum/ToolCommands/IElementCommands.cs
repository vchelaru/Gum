using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ToolCommands;
public interface IElementCommands
{
    #region Instance
    InstanceSave AddInstance(ElementSave elementToAddTo, string name, string? type = null, string? parentName = null, int? desiredIndex = null);

    InstanceSave AddInstance(ElementSave elementToAddTo, InstanceSave instanceSave, string? parentName = null, int? desiredIndex = null);

    void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom);

    void RemoveInstances(List<InstanceSave> instances, ElementSave elementToRemoveFrom);

    #endregion

    #region State

    StateSave AddState(IStateContainer stateContainer, StateSaveCategory category, string name);

    void AddState(IStateContainer stateContainer, StateSaveCategory category, StateSave stateSave, int? desiredIndex = null);

    void RemoveState(StateSave stateSave, IStateContainer elementToRemoveFrom);

    #endregion

    #region Variables

    void SortVariables();

    void SortVariables(IStateContainer container);

    bool MoveSelectedObjectsBy(float xToMoveBy, float yToMoveBy);

    bool ShouldSkipDraggingMovementOn(InstanceSave instanceSave);

    float ModifyVariable(string baseVariableName, float modificationAmount, InstanceSave instanceSave);

    float ModifyVariable(string baseVariableName, float modificationAmount, ElementSave elementSave);

    object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave);


    #endregion

    #region Category

    StateSaveCategory AddCategory(IStateContainer objectToAddTo, string name);

    #endregion

    #region Behavior

    BehaviorInstanceSave AddInstance(BehaviorSave behaviorToAddTo, string name, string type = null, string parentName = null);

    void AddBehaviorTo(BehaviorSave behavior, ComponentSave componentSave, bool performSave = true);

    void AddBehaviorTo(string behaviorName, ComponentSave componentSave, bool performSave = true);

    #endregion

    void RemoveParentReferencesToInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom);
}
