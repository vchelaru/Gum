using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Errors;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins;

public interface IPluginManager
{
    // todo - interface this...
    void BeforeSavingElementSave(ElementSave savedElement);

    void AfterSavingElementSave(ElementSave savedElement);

    void BeforeSavingProjectSave(GumProjectSave savedProject);

    void ProjectLoad(GumProjectSave newlyLoadedProject);

    void ProjectPropertySet(string propertyName);

    void ProjectSave(GumProjectSave savedProject);

    GraphicalUiElement CreateGraphicalUiElement(ElementSave elementSave);

    void ProjectLocationSet(FilePath filePath);

    void Export(ElementSave elementToExport);

    void ModifyDefaultStandardState(string type, StateSave stateSave);

    bool TryHandleDelete();

    void ShowDeleteDialog(DeleteOptionsWindow window, Array objectsToDelete);

    void DeleteConfirm(DeleteOptionsWindow window, Array objectsToDelete);

    void ElementRename(ElementSave elementSave, string oldName);

    void ElementAdd(ElementSave element);

    void ElementDelete(ElementSave element);

    void ElementDuplicate(ElementSave oldElement, ElementSave newElement);

    void StateRename(StateSave stateSave, string oldName);
    void StateAdd(StateSave stateSave);

    void StateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory);

    void StateDelete(StateSave stateSave);

    void ReactToStateSaveSelected(StateSave? stateSave);

    void ReactToCustomStateSaveSelected(StateSave stateSave);
    void RefreshStateTreeView();
    void RefreshElementTreeView(IInstanceContainer? instanceContainer = null);
    void CategoryRename(StateSaveCategory category, string oldName);
    void CategoryAdd(StateSaveCategory category);
    void CategoryDelete(StateSaveCategory category);
    void ReactToStateSaveCategorySelected(StateSaveCategory? category);
    void VariableAdd(ElementSave elementSave, string variableName);
    void VariableDelete(ElementSave elementSave, string variableName);
    /// <summary>
    /// Raised when a variable is set.
    /// </summary>
    /// <param name="parentElement">The element that contains the variable, or which contains the instance that holds the variable.</param>
    /// <param name="instance">The optional instance that holds the variable</param>
    /// <param name="unqualifiedChangedMemberName">The unqualified name. If an instance's value is set, this would be unqualified, such as "X" instead of "SpriteInstance.X"</param>
    /// <param name="oldValue">The value prior to being set.</param>
    void VariableSet(ElementSave parentElement, InstanceSave? instance, string unqualifiedChangedMemberName, object? oldValue);
    void VariableSelected(IStateContainer container, VariableSave variable);
    void VariableRemovedFromCategory(string variableName, StateSaveCategory category);
    void InstanceRename(ElementSave element, InstanceSave instanceSave, string oldName);
    void AfterUndo();
    List<Attribute> GetAttributesFor(VariableSave variableSave);
    void ElementSelected(ElementSave? elementSave);
















    ITreeNode? GetTreeNodeOver();
    IEnumerable<ITreeNode> GetSelectedNodes();
    void BehaviorSelected(BehaviorSave? behaviorSave);
    void BehaviorReferenceSelected(ElementBehaviorReference behaviorReference, ElementSave elementSave);
    void BehaviorVariableSelected(VariableSave variable);
    void BehaviorCreated(BehaviorSave behavior);
    void BehaviorDeleted(BehaviorSave behavior);
    void InstanceSelected(ElementSave elementSave, InstanceSave instance);
    void InstanceAdd(ElementSave elementSave, InstanceSave instance);
    void InstanceDelete(ElementSave elementSave, InstanceSave instance);

    void InstancesDelete(ElementSave elementSave, InstanceSave[] instances);
    StateSave? GetDefaultStateFor(string type);
    void InstanceReordered(InstanceSave instance);
    bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember);
    void RefreshBehaviorView(ElementSave elementSave);











    IEnumerable<IPositionedSizedObject>? GetSelectedIpsos();
    System.Numerics.Vector2? GetWorldCursorPosition(InputLibrary.Cursor cursor);
    void FillWithErrors(List<ErrorViewModel> errors, PluginBase? plugin = null);
    bool GetIfShouldSuppressRemoveEditorHighlight();
    void FocusSearch();
    bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf);
}
