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






    List<Attribute> GetAttributesFor(VariableSave variableSave);





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
