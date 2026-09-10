using System;
using System.Collections.Generic;
using System.Numerics;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Responses;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilities;

namespace Gum.Avalonia.Services;

/// <summary>
/// A plugin host with no plugins. Lets the head compose and start before phase 40 brings the MEF
/// plugin host across; every notification is a no-op and every query answers "nothing".
/// </summary>
public class NullPluginManager :
    IPluginManager, IUndoPluginNotifier, IDeletePluginNotifier, ICopyPastePluginNotifier, IRenamePluginNotifier
{
    private readonly Shell.ShellViewModel _shell;

    /// <summary>Creates the host; it keeps the window title current since no plugin does yet.</summary>
    public NullPluginManager(Shell.ShellViewModel shell)
    {
        _shell = shell;
    }

    /// <inheritdoc/>
    public bool IsInitialized => true;

    /// <inheritdoc/>
    public void BeforeSavingElementSave(ElementSave savedElement) { }
    /// <inheritdoc/>
    public void AfterSavingElementSave(ElementSave savedElement) { }
    /// <inheritdoc/>
    public void BeforeSavingProjectSave(GumProjectSave savedProject) { }
    /// <inheritdoc/>
    public void ProjectLoad(GumProjectSave newlyLoadedProject) => UpdateTitle(newlyLoadedProject);
    /// <inheritdoc/>
    public void ProjectPropertySet(string propertyName) { }
    /// <inheritdoc/>
    public void ProjectSave(GumProjectSave savedProject) => UpdateTitle(savedProject);

    private void UpdateTitle(GumProjectSave? project) =>
        _shell.Title = project != null && !string.IsNullOrEmpty(project.FullFileName) ? project.FullFileName : "Gum";
    // The MEF host also answers null when no plugin creates the element; callers already handle it.
    /// <inheritdoc/>
    public GraphicalUiElement CreateGraphicalUiElement(ElementSave elementSave) => null!;
    /// <inheritdoc/>
    public void ProjectLocationSet(FilePath filePath) { }
    /// <inheritdoc/>
    public void Export(ElementSave elementToExport) { }
    /// <inheritdoc/>
    public void ModifyDefaultStandardState(string type, StateSave stateSave) { }
    /// <inheritdoc/>
    public bool TryHandleDelete() => false;
    /// <inheritdoc/>
    public void ElementRename(ElementSave elementSave, string oldName) { }
    /// <inheritdoc/>
    public void ElementAdd(ElementSave element) { }
    /// <inheritdoc/>
    public void ElementDelete(ElementSave element) { }
    /// <inheritdoc/>
    public void ElementImported(ElementSave element) { }
    /// <inheritdoc/>
    public void ElementDuplicate(ElementSave oldElement, ElementSave newElement) { }
    /// <inheritdoc/>
    public void ElementReloaded(ElementSave element) { }
    /// <inheritdoc/>
    public void StateRename(StateSave stateSave, string oldName) { }
    /// <inheritdoc/>
    public void StateAdd(StateSave stateSave) { }
    /// <inheritdoc/>
    public void StateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory) { }
    /// <inheritdoc/>
    public void StateDelete(StateSave stateSave) { }
    /// <inheritdoc/>
    public void ReactToStateSaveSelected(StateSave? stateSave) { }
    /// <inheritdoc/>
    public void ReactToFileChanged(FilePath filePath) { }
    /// <inheritdoc/>
    public void ReactToCustomStateSaveSelected(StateSave stateSave) { }
    /// <inheritdoc/>
    public void RefreshStateTreeView() { }
    /// <inheritdoc/>
    public void RefreshElementTreeView(IInstanceContainer? instanceContainer = null) { }
    /// <inheritdoc/>
    public void CategoryRename(StateSaveCategory category, string oldName) { }
    /// <inheritdoc/>
    public void CategoryAdd(StateSaveCategory category) { }
    /// <inheritdoc/>
    public void CategoryDelete(StateSaveCategory category) { }
    /// <inheritdoc/>
    public DeleteResponse GetDeleteStateCategoryResponse(StateSaveCategory stateSaveCategory, IStateContainer element) => new DeleteResponse { ShouldDelete = true };
    /// <inheritdoc/>
    public DeleteResponse GetDeleteStateResponse(StateSave stateSave, IStateContainer element) => new DeleteResponse { ShouldDelete = true };
    /// <inheritdoc/>
    public void ReactToStateSaveCategorySelected(StateSaveCategory? category) { }
    /// <inheritdoc/>
    public void VariableAdd(ElementSave elementSave, string variableName) { }
    /// <inheritdoc/>
    public void VariableDelete(ElementSave elementSave, string variableName) { }
    /// <inheritdoc/>
    public void VariableSet(ElementSave parentElement, InstanceSave? instance, string unqualifiedChangedMemberName, object? oldValue) { }
    /// <inheritdoc/>
    public void VariableSelected(IStateContainer container, VariableSave variable) { }
    /// <inheritdoc/>
    public void VariableRemovedFromCategory(string variableName, StateSaveCategory category) { }
    /// <inheritdoc/>
    public void InstanceRename(ElementSave element, InstanceSave instanceSave, string oldName) { }
    /// <inheritdoc/>
    public void AfterUndo() { }
    /// <inheritdoc/>
    public List<Attribute> GetAttributesFor(VariableSave variableSave) => new List<Attribute>();
    /// <inheritdoc/>
    public void ElementSelected(ElementSave? elementSave) { }
    /// <inheritdoc/>
    public ITreeNode? GetTreeNodeOver() => null;
    /// <inheritdoc/>
    public IEnumerable<ITreeNode> GetSelectedNodes() => Array.Empty<ITreeNode>();
    /// <inheritdoc/>
    public void BehaviorSelected(BehaviorSave? behaviorSave) { }
    /// <inheritdoc/>
    public void BehaviorReferenceSelected(ElementBehaviorReference behaviorReference, ElementSave elementSave) { }
    /// <inheritdoc/>
    public void BehaviorVariableSelected(VariableSave variable) { }
    /// <inheritdoc/>
    public void BehaviorCreated(BehaviorSave behavior) { }
    /// <inheritdoc/>
    public void BehaviorDeleted(BehaviorSave behavior) { }
    /// <inheritdoc/>
    public void InstanceSelected(ElementSave elementSave, InstanceSave instance) { }
    /// <inheritdoc/>
    public void InstanceAdd(ElementSave elementSave, InstanceSave instance) { }
    /// <inheritdoc/>
    public void InstanceDelete(ElementSave elementSave, InstanceSave instance) { }
    /// <inheritdoc/>
    public void BehaviorInstanceAdd(BehaviorSave behavior, BehaviorInstanceSave instance) { }
    /// <inheritdoc/>
    public void BehaviorInstanceDelete(BehaviorSave behavior, BehaviorInstanceSave instance) { }
    /// <inheritdoc/>
    public void BehaviorInstanceRename(BehaviorSave behavior, BehaviorInstanceSave instance) { }
    /// <inheritdoc/>
    public void InstancesDelete(ElementSave elementSave, InstanceSave[] instances) { }
    /// <inheritdoc/>
    public StateSave? GetDefaultStateFor(string type) => null;
    /// <inheritdoc/>
    public void InstanceReordered(InstanceSave instance) { }
    /// <inheritdoc/>
    public bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember) => true;
    /// <inheritdoc/>
    public void RefreshBehaviorView(ElementSave elementSave) { }
    /// <inheritdoc/>
    public IEnumerable<IPositionedSizedObject>? GetSelectedIpsos() => null;
    /// <inheritdoc/>
    public Vector2? GetWorldCursorPosition() => null;
    /// <inheritdoc/>
    public void FillWithErrors(List<ErrorViewModel> errors, object? plugin = null) { }
    /// <inheritdoc/>
    public void FillTopLevelNames(ElementSave element, List<TopLevelName> names) { }
    /// <inheritdoc/>
    public bool GetIfShouldSuppressRemoveEditorHighlight() => false;
    /// <inheritdoc/>
    public void FocusSearch() { }
    /// <inheritdoc/>
    public void FocusVariableFilter() { }
    /// <inheritdoc/>
    public bool ShouldExclude(VariableSave defaultVariable, RecursiveVariableFinder rvf) => false;
    /// <inheritdoc/>
    public void HighlightTreeNode(IPositionedSizedObject? positionedSizedObject) { }
    /// <inheritdoc/>
    public void HandleWireframeResized() { }
    /// <inheritdoc/>
    public void CameraChanged() { }
    /// <inheritdoc/>
    public void BeforeRender() { }
    /// <inheritdoc/>
    public void AfterRender() { }
    /// <inheritdoc/>
    public void BehaviorReferencesChanged(ElementSave elementSave) { }
    /// <inheritdoc/>
    public void RefreshVariableView(bool force) { }
    /// <inheritdoc/>
    public void WireframePropertyChanged(string propertyName) { }
    /// <inheritdoc/>
    public IRenderableIpso CreateRenderableForType(string type) => new InvisibleRenderable();
    /// <inheritdoc/>
    public void WireframeRefreshed() { }
    /// <inheritdoc/>
    public void TreeNodeSelected(object? treeNode) { }
    /// <inheritdoc/>
    public void SetHighlightedIpso(GraphicalUiElement? positionedSizedObject) { }
    /// <inheritdoc/>
    public IReadOnlyList<PluginSummary> GetAllPluginSummaries() => Array.Empty<PluginSummary>();
    /// <inheritdoc/>
    public PluginScanReport? GetPluginScanReport() => null;
    /// <inheritdoc/>
    public PluginSummary DisableUserPlugin(object pluginHandle) => throw new InvalidOperationException("No plugins are loaded in this head yet.");
    /// <inheritdoc/>
    public PluginSummary TryEnablePlugin(object pluginHandle) => throw new InvalidOperationException("No plugins are loaded in this head yet.");
}
