using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Responses;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
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

    void ElementRename(ElementSave elementSave, string oldName);

    void ElementAdd(ElementSave element);

    void ElementDelete(ElementSave element);

    void ElementImported(ElementSave element);

    void ElementDuplicate(ElementSave oldElement, ElementSave newElement);

    void ElementReloaded(ElementSave element);

    void StateRename(StateSave stateSave, string oldName);
    void StateAdd(StateSave stateSave);

    void StateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory);

    void StateDelete(StateSave stateSave);

    void ReactToStateSaveSelected(StateSave? stateSave);

    void ReactToFileChanged(FilePath filePath);

    void ReactToCustomStateSaveSelected(StateSave stateSave);
    void RefreshStateTreeView();
    void RefreshElementTreeView(IInstanceContainer? instanceContainer = null);
    void CategoryRename(StateSaveCategory category, string oldName);
    void CategoryAdd(StateSaveCategory category);
    void CategoryDelete(StateSaveCategory category);
    DeleteResponse GetDeleteStateCategoryResponse(StateSaveCategory stateSaveCategory, IStateContainer element);
    DeleteResponse GetDeleteStateResponse(StateSave stateSave, IStateContainer element);
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
    void BehaviorInstanceAdd(BehaviorSave behavior, BehaviorInstanceSave instance);
    void BehaviorInstanceDelete(BehaviorSave behavior, BehaviorInstanceSave instance);
    void BehaviorInstanceRename(BehaviorSave behavior, BehaviorInstanceSave instance);

    void InstancesDelete(ElementSave elementSave, InstanceSave[] instances);
    StateSave? GetDefaultStateFor(string type);
    void InstanceReordered(InstanceSave instance);
    bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember);
    void RefreshBehaviorView(ElementSave elementSave);











    IEnumerable<IPositionedSizedObject>? GetSelectedIpsos();

    /// <summary>
    /// Looks up the world position under the current cursor via plugins that can answer it (the
    /// wireframe/editor host). Parameterless rather than taking the tool-only, WinForms/KNI-coupled
    /// <c>InputLibrary.Cursor</c> so this interface can live in the headless Gum.Presentation
    /// assembly; the implementation resolves the singleton <c>InputLibrary.Cursor.Self</c> itself.
    /// </summary>
    System.Numerics.Vector2? GetWorldCursorPosition();

    /// <summary>
    /// Fills <paramref name="errors"/> with plugin-contributed errors. Typed <see cref="object"/>
    /// rather than the tool-only <c>PluginBase</c> so this interface can live in the headless
    /// Gum.Presentation assembly; pass a plugin instance to scope the check to that plugin, or
    /// omit it to run every plugin's error check.
    /// </summary>
    void FillWithErrors(List<ErrorViewModel> errors, object? plugin = null);
    void FillTopLevelNames(ElementSave element, List<TopLevelName> names);
    bool GetIfShouldSuppressRemoveEditorHighlight();
    void FocusSearch();
    bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf);

    // Widened for #3753: MainBehaviorsPlugin/MainEditorTabPlugin previously took the concrete
    // PluginManager as a ctor param because these six weren't on the interface yet.
    void HighlightTreeNode(IPositionedSizedObject? positionedSizedObject);
    void HandleWireframeResized();
    void CameraChanged();
    void BeforeRender();
    void AfterRender();
    void BehaviorReferencesChanged(ElementSave elementSave);

    // Widened for #3753 (concrete-field cleanup pass): SelectedState, ElementCommands, EditCommands,
    // GuiCommands, WireframeCommands, WireframeObjectManager, and ElementTreeViewManager previously took
    // the concrete PluginManager as a ctor/field type because these weren't on the interface yet.
    void RefreshVariableView(bool force);
    void WireframePropertyChanged(string propertyName);
    IRenderableIpso CreateRenderableForType(string type);
    void WireframeRefreshed();
    // Sealed to object (not System.Windows.Forms.TreeNode) per the WinForms/WPF interface-leak ratchet
    // (UiDecouplingRatchetTests) -- same pattern as ITabManager.AddControl.
    void TreeNodeSelected(object? treeNode);
    bool IsInitialized { get; }
    void SetHighlightedIpso(GraphicalUiElement? positionedSizedObject);

    // Widened for #3754: PluginsDialogViewModel (the "Manage Plugins" dialog) previously called
    // the concrete PluginManager's static AllPluginContainers/ShutDownPlugin/ReenablePlugin members
    // directly. IPlugin/PluginContainer live in the tool-only Gum.csproj (which this headless
    // Gum.Presentation assembly cannot reference), so plugin identity crosses this interface as the
    // opaque PluginSummary.PluginHandle token instead - see PluginSummary.

    /// <summary>
    /// Returns a snapshot of every loaded plugin, for the "Manage Plugins" dialog.
    /// </summary>
    IReadOnlyList<PluginSummary> GetAllPluginSummaries();

    /// <summary>
    /// Disables the plugin identified by <paramref name="pluginHandle"/> as user-initiated
    /// (persisting the disabled state) and returns its updated summary.
    /// </summary>
    PluginSummary DisableUserPlugin(object pluginHandle);

    /// <summary>
    /// Re-enables a previously-disabled plugin identified by <paramref name="pluginHandle"/>,
    /// retrying its <c>StartUp()</c>, and returns its updated summary.
    /// </summary>
    PluginSummary TryEnablePlugin(object pluginHandle);
}
