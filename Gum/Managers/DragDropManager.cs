using CommonFormsAndControls;
using Gum.Commands;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Managers;


public class DragDropManager : IDragDropManager
{
    /// <summary>
    /// Drag-and-drop data format used by the Standards chip palette. The payload is the standard
    /// type name (e.g. "Text"). Shared by the WPF chip (drag source) and the WinForms tree /
    /// XNA wireframe drop targets so they can recognize a chip drag across the WPF/WinForms boundary.
    /// </summary>
    public const string StandardElementNameDataFormat = "GumStandardElementName";

    #region Fields

    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly IUndoManager _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly ICopyPasteLogic _copyPasteLogic;
    private readonly IImportLogic _importLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IPluginManager _pluginManager;
    private readonly IReorderLogic _reorderLogic;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;

    #endregion

    #region Properties

    public InputLibrary.Cursor Cursor
    {
        get { return InputLibrary.Cursor.Self; }
    }


    #endregion

    #region Constructor

    public DragDropManager(ICircularReferenceManager circularReferenceManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IRenameLogic renameLogic,
        IUndoManager undoManager,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ICopyPasteLogic copyPasteLogic,
        IImportLogic importLogic,
        IWireframeObjectManager wireframeObjectManager,
        IPluginManager pluginManager,
        IReorderLogic reorderLogic,
        IProjectManager projectManager,
        IProjectState projectState)
    {
        _circularReferenceManager = circularReferenceManager;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _renameLogic = renameLogic;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _copyPasteLogic = copyPasteLogic;
        _importLogic = importLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _pluginManager = pluginManager;
        _reorderLogic = reorderLogic;
        _projectManager = projectManager;
        _projectState = projectState;
    }

    #endregion

    #region Drag+drop File (from windows explorer)

    public IEnumerable<string> ValidTextureExtensions
    {
        get
        {
            yield return "png";
            yield return "jpg";
            yield return "tga";
            yield return "gif";
            yield return "svg";
            yield return "bmp";
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> ValidFontExtensions
    {
        get
        {
            yield return "ttf";
        }
    }

    public bool IsValidExtensionForFileDrop(string file)
    {
        string extension = FileManager.GetExtension(file);
        return ValidTextureExtensions.Contains(extension) || ValidFontExtensions.Contains(extension);
    }

    #endregion

    #region Drop Element (like components) on TreeView

    /// <inheritdoc/>
    public void HandleDroppedStandardElementOnTreeNode(StandardElementSave standardElement, ITreeNode targetTreeNode)
    {
        // Reuse the exact same path as dragging a Standard element node onto a Screen/Component.
        // Build an Append DropTarget describing the drop target: a null DropTarget would skip
        // HandleDroppedElementSave's onto-instance branch (which parents the new instance to the
        // target instance AND refreshes the wireframe afterward). Since AddInstance refreshes the
        // wireframe BEFORE writing the Parent variable, skipping that branch leaves a chip dropped
        // onto an instance visually un-parented until the next refresh (#973).
        DropTarget? dropTarget = targetTreeNode.Tag switch
        {
            InstanceSave targetInstance => new DropTarget(targetInstance.ParentContainer, targetInstance, new DropPosition.Append()),
            ElementSave targetElement => new DropTarget(targetElement, null, new DropPosition.Append()),
            _ => null
        };

        using var undoLock = _undoManager.RequestLock();
        HandleDroppedElementSave(standardElement, targetTreeNode, targetTreeNode.Tag, targetTreeNode, dropTarget);
    }

    private void HandleDroppedElementSave(object draggedComponentOrElement, ITreeNode treeNodeDroppedOn, object targetTag, ITreeNode targetTreeNode, DropTarget? dropTarget)
    {
        ElementSave draggedAsElementSave = draggedComponentOrElement as ElementSave;

        // User dragged an element save - so they want to take something like a
        // text object and make an instance in another element like a Screen

        if (targetTag is ElementSave)
        {
            HandleDroppedElementInElement(draggedAsElementSave, targetTag as ElementSave, null, dropTarget);
        }
        else if (targetTag is InstanceSave)
        {
            // The user dropped it on an instance save, but likely meant to drop
            // it as an object under the current element.

            InstanceSave targetInstance = targetTag as InstanceSave;

            // When a parent is set, we normally raise an event for that. This is a tricky situation because
            // we need to set the parent before adding the object.

            var newInstance = HandleDroppedElementInElement(draggedAsElementSave, targetInstance.ParentContainer, targetInstance, dropTarget);

            if(newInstance != null && dropTarget != null)
            {
                // Since the user dropped on another instance, let's try to parent it:
                HandleDroppingInstanceOnTarget(newInstance, targetTreeNode, dropTarget);

                // HandleDroppingInstanceOnTarget internally calls
                // _wireframeObjectManager.RefreshAll, but since
                // the Parent is set in HandleDroppedElementInElement,
                // then HandleDroppingInstanceOnTarget does not report the
                // parent as having changed. We need to still force a refresh
                // to make the parenting apply in the wireframe display.
                _wireframeObjectManager.RefreshAll(true, forceReloadTextures: false);
            }

        }
        else if (treeNodeDroppedOn.IsTopComponentContainerTreeNode())
        {
            HandleDroppedElementOnTopComponentTreeNode(draggedAsElementSave);

        }
        else if (draggedAsElementSave is ComponentSave && treeNodeDroppedOn.IsPartOfComponentsFolderStructure())
        {
            HandleDroppedElementOnFolder(draggedAsElementSave, treeNodeDroppedOn);
        }
        else if(draggedAsElementSave is ScreenSave && treeNodeDroppedOn.IsPartOfScreensFolderStructure())
        {
            HandleDroppedElementOnFolder(draggedAsElementSave, treeNodeDroppedOn);
        }
        else if(draggedAsElementSave is ScreenSave == false && targetTag is BehaviorSave targetBehavior)
        {
            HandleDroppedElementOnBehavior(draggedAsElementSave, targetBehavior);
        }
        else if(draggedAsElementSave is ScreenSave && targetTag is BehaviorSave)
        {
            _dialogService.ShowMessage("Screens cannot be added as required instances in behaviors");
        }
        else
        {
            _dialogService.ShowMessage("You must drop " + draggedAsElementSave.Name + " on either a Screen or an Component");
        }
    }

    private void HandleDroppedElementOnFolder(ElementSave draggedAsElementSave, ITreeNode treeNodeDroppedOn)
    {
        if(draggedAsElementSave is StandardElementSave)
        {
            _dialogService.ShowMessage("Cannot move standard elements to different folders");
        }
        else
        {
            var fullFolderPath = treeNodeDroppedOn.GetFullFilePath();

            var fullElementFilePath = draggedAsElementSave.GetFullPathXmlFile().GetDirectoryContainingThis();

            if(fullFolderPath != fullElementFilePath)
            {
                var projectFolder = FileManager.GetDirectory(_projectManager.GumProjectSave.FullFileName);

                string nodeRelativeToProject = FileManager.MakeRelative(fullFolderPath.FullPath, projectFolder + draggedAsElementSave.Subfolder + "/", preserveCase:true)
                    .Replace("\\", "/");

                string oldName = draggedAsElementSave.Name;
                draggedAsElementSave.Name = nodeRelativeToProject + FileManager.RemovePath(draggedAsElementSave.Name);
                _renameLogic.HandleRename(draggedAsElementSave, (InstanceSave)null,  oldName, NameChangeAction.Move);
            }

        }

    }

    private void HandleDroppedElementOnTopComponentTreeNode(ElementSave draggedAsElementSave)
    {
        string name = draggedAsElementSave.Name;

        string currentDirectory = FileManager.GetDirectory(draggedAsElementSave.Name);

        if(!string.IsNullOrEmpty(currentDirectory))
        {
            // It's in a directory, we're going to move it out
            draggedAsElementSave.Name = FileManager.RemovePath(name);
            _renameLogic.HandleRename(draggedAsElementSave, (InstanceSave)null, name, NameChangeAction.Move);
        }
    }

    private InstanceSave HandleDroppedElementOnBehavior(ElementSave draggedElement, BehaviorSave behavior)
    {
        InstanceSave newInstance = null;

        string errorMessage = null;

        //handled = false;

        //errorMessage = GetDropElementErrorMessage(draggedAsElementSave, target, errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            _dialogService.ShowMessage(errorMessage);
        }
        else
        {
#if DEBUG
            if (draggedElement == null)
            {
                throw new Exception("draggedElement is null and it shouldn't be.  For vic - try to put this exception earlier to see what's up.");
            }
#endif

            string name = _elementCommands.GetUniqueNameForNewInstance(draggedElement, behavior);

            // Capture the pre-change state for undo. We bypass the undo lock here because
            // OnNodeSortingDropped already holds a lock, which would normally block
            // RecordBehaviorState(). We need the snapshot before any change occurs.
            _undoManager.RecordBehaviorState(behavior);

            // First we want to re-select the target so that it is highlighted in the tree view and not
            // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
            _selectedState.SelectedBehavior = behavior;

            newInstance = _elementCommands.AddInstance(behavior, name, draggedElement.Name);
            //handled = true;
        }

        return newInstance;
    }

    private InstanceSave HandleDroppedElementInElement(ElementSave draggedAsElementSave, ElementSave target, InstanceSave parentInstance, DropTarget? dropTarget)
    {
        InstanceSave newInstance = null;

        string errorMessage = null;

        errorMessage = GetDropElementErrorMessage(draggedAsElementSave, target, errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            _dialogService.ShowMessage(errorMessage);
        }
        else
        {
#if DEBUG
            if (draggedAsElementSave == null)
            {
                throw new Exception("DraggedAsElementSave is null and it shouldn't be.  For vic - try to put this exception earlier to see what's up.");
            }
#endif

            string name = _elementCommands.GetUniqueNameForNewInstance(draggedAsElementSave, target);

            // First we want to re-select the target so that it is highlighted in the tree view and not
            // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
            _selectedState.SelectedElement = target;

            int? desiredIndex = ResolveDesiredFlatIndex(dropTarget?.Position, target);

            newInstance = _elementCommands.AddInstance(target, name, draggedAsElementSave.Name, parentInstance?.Name, desiredIndex);
        }

        return newInstance;
    }

    /// <summary>
    /// Translate a <see cref="DropPosition"/> into a flat-list index inside
    /// <paramref name="element"/>.<see cref="ElementSave.Instances"/>. Returns
    /// null for <see cref="DropPosition.Append"/>, which lets callers like
    /// <c>AddInstance</c> use their "append to end" default path.
    /// </summary>
    private static int? ResolveDesiredFlatIndex(DropPosition? position, ElementSave element)
    {
        return position switch
        {
            null => null,
            DropPosition.Append => null,
            DropPosition.InsertAt at => Math.Clamp(at.Index, 0, element.Instances.Count),
            DropPosition.BeforeSibling before => Math.Max(0, element.Instances.IndexOf(before.Sibling)),
            DropPosition.AfterSibling after => element.Instances.IndexOf(after.Sibling) + 1,
            _ => null
        };
    }

    private string? GetDropElementErrorMessage(ElementSave draggedAsElementSave, ElementSave target, string errorMessage)
    {
        if (target == null)
        {
            errorMessage = "No Screen or Component selected";
        }

        if (errorMessage == null && target is StandardElementSave)
        {
            // do nothing, it's annoying:
            errorMessage = $"Standard type {target} cannot contain objects instances, so {draggedAsElementSave} cannot be dropped here";
        }

        if (errorMessage == null && draggedAsElementSave is ScreenSave)
        {
            errorMessage = "Screens can't be dropped into other Screens or Components";
        }

        if (errorMessage == null)
        {
            if(!_circularReferenceManager.CanTypeBeAddedToElement(target!, draggedAsElementSave.Name))
            {
                errorMessage = $"Cannot add {draggedAsElementSave.Name} to {target!.Name} because it would create a circular reference";
            }
        }


        if (errorMessage == null && target!.IsSourceFileMissing)
        {
            errorMessage = "The source file for " + target.Name + " is missing, so it cannot be edited";
        }

        if(errorMessage == null && target == _selectedState.SelectedElement)
        {
            if(_selectedState.SelectedStateSave != _selectedState.SelectedElement.DefaultState)
            {
                errorMessage = $"Cannot add instances to " +
                    $"{_selectedState.SelectedElement} while the {_selectedState.SelectedStateSave} " +
                    $"state is selected. Select the Default state first.";
            }
        }

        return errorMessage;
    }


    #endregion

    #region Drop BehaviorSave

    private void HandleDroppedBehavior(BehaviorSave behavior, ITreeNode treeNodeDroppedOn)
    {
        var targetTag = treeNodeDroppedOn.Tag;

        var targetComponent = targetTag as ComponentSave;

        //////////////////Early Out///////////////
        if(targetComponent == null)
        {
            return;
        }
        var alreadyHasBehavior = targetComponent.Behaviors.Any(item => item.BehaviorName == behavior.Name);

        if(alreadyHasBehavior)
        {
            return;
        }
        ///////////////End Early Out//////////////

        // This can happen if the user drags a behavior onto a component
        // which is not currently selected. We need it to be selected for
        // undos to record properly, so let's select it first:
        _selectedState.SelectedComponent = targetComponent;

        using var undoLock = _undoManager.RequestLock();


        _elementCommands.AddBehaviorTo(behavior, targetComponent);

        if(targetComponent == _selectedState.SelectedComponent)
        {
            _guiCommands.RefreshStateTreeView();
            _guiCommands.BroadcastRefreshBehaviorView();
        }
    }

    #endregion

    #region Drop Instance on TreeNode

    private void HandleDroppedInstance(object draggedObject, ITreeNode targetTreeNode, DropTarget? dropTarget)
    {
        object targetObject = targetTreeNode.Tag;

        InstanceSave draggedAsInstanceSave = (InstanceSave)draggedObject;

        var targetElementSave = targetObject as ElementSave;
        var targetInstanceSave = targetObject as InstanceSave;
        if (targetElementSave == null && targetInstanceSave != null)
        {
            targetElementSave = targetInstanceSave.ParentContainer;
        }


        bool isSameElement = draggedAsInstanceSave != null && targetElementSave == draggedAsInstanceSave.ParentContainer;

        if (targetElementSave != null)
        {
            var canBeAdded = true;

            if(draggedAsInstanceSave != null)
            {
                canBeAdded = _circularReferenceManager.CanTypeBeAddedToElement(targetElementSave, draggedAsInstanceSave.BaseType);
            }

            if (!canBeAdded)
            {
                _dialogService.ShowMessage($"Cannot add {draggedAsInstanceSave.Name} " +
                    $"to {targetElementSave.Name} because it would create a circular reference");
                return;
            }

            else if (isSameElement)
            {
                if (dropTarget != null)
                {
                    HandleDroppingInstanceOnTarget(draggedAsInstanceSave, targetTreeNode, dropTarget);
                }
            }
            else
            {
                List<InstanceSave> instances = new List<InstanceSave>() { draggedAsInstanceSave };
                List<StateSave> stateWithVariablesForOriginalInstance = new List<StateSave>
                {
                    draggedAsInstanceSave.ParentContainer?.DefaultState.Clone() ?? new StateSave()
                };

                _copyPasteLogic.ForceSelectionChanged();

                // by creating a forced selected state,
                // we can precisely control the target for
                // the paste without having to actually change
                // the selection which would have side effects app-wide.

                SelectedStateSnapshot forcedSelectedState = new SelectedStateSnapshot
                {
                    SelectedElement = targetElementSave,
                    SelectedStateSave = targetElementSave.DefaultState,
                    SelectedInstance = targetInstanceSave
                };

                //var forcedSelectedState = _selectedState;

                var newInstances = _copyPasteLogic.PasteInstanceSaves(instances,
                    stateWithVariablesForOriginalInstance,
                    targetElementSave,
                    targetInstanceSave,
                    forcedSelectedState);

                // January 17, 2025
                // For now, let's just
                // handle the most common
                // case - dropping a single
                // instance. We can handle multiples
                // later, but this is a rarer case and
                // this bug fix has already dragged on too
                // long. The unit test for one case is here:
                // DragDropManagerTests.OnNodeSortingDropped_DropInstance_ShouldInsertAtIndex_OnDifferentElement
                var firstInstance = newInstances.FirstOrDefault();
                int desiredFlatIndex = ResolveDesiredFlatIndex(dropTarget?.Position, targetElementSave)
                    ?? targetElementSave.Instances.Count;
                if(firstInstance != null && targetElementSave.Instances.IndexOf(firstInstance) != desiredFlatIndex)
                {
                    targetElementSave.Instances.Remove(firstInstance);
                    int safeIndex = Math.Min(desiredFlatIndex, targetElementSave.Instances.Count);
                    targetElementSave.Instances.Insert(safeIndex, firstInstance);

                    _reorderLogic.RefreshInResponseToReorder(firstInstance);
                }

                _selectedState.SelectedInstances = newInstances;
            }
        }
        else if(targetObject is BehaviorSave asBehaviorSave)
        {
            HandleDroppingInstanceOnBehaviorSave(draggedAsInstanceSave, asBehaviorSave);
        }
    }

    private void HandleDroppingInstanceOnBehaviorSave(InstanceSave draggedAsInstanceSave, BehaviorSave asBehaviorSave)
    {
        // Capture the pre-change state for undo (bypasses the undo lock held by OnNodeSortingDropped).
        _undoManager.RecordBehaviorState(asBehaviorSave);

        _selectedState.SelectedBehavior = asBehaviorSave;

        _elementCommands.AddInstance(asBehaviorSave, draggedAsInstanceSave.Name, draggedAsInstanceSave.BaseType);
    }

    private void HandleDroppingInstanceOnTarget(InstanceSave dragDroppedInstance, ITreeNode? targetTreeNode, DropTarget dropTarget)
    {
        if (dragDroppedInstance.DefinedByBase)
        {
            object describedTarget = (object?)dropTarget.ParentInstance ?? dropTarget.ParentElement;
            _dialogService.ShowMessage($"{dragDroppedInstance.Name} cannot be added as a child of {describedTarget} because it is defined in a base element");
            return;
        }

        ElementSave targetElementSave = dropTarget.ParentElement;
        InstanceSave? parentInstance = dropTarget.ParentInstance;
        string variableName = dragDroppedInstance.Name + ".Parent";

        string? parentName;
        if (parentInstance != null)
        {
            parentName = parentInstance.Name;
            string defaultChild = ObjectFinder.Self.GetDefaultChildName(parentInstance, _selectedState.SelectedStateSave);
            if (!string.IsNullOrEmpty(defaultChild))
            {
                parentName += "." + defaultChild;
            }
        }
        else
        {
            // drag+drop on the container, so detach:
            parentName = null;
        }

        // No-op when dropping an instance on a parent it already has. For
        // Append (the kind=Into drop kind), the user's intent is "make this a
        // child of parent" — already satisfied — so the flat-list reorder is
        // a surprising side effect. Explicit BeforeSibling/AfterSibling drops
        // still reorder because the user picked a specific new position.
        if (dropTarget.Position is DropPosition.Append &&
            ParentVariableAlreadyMatches(targetElementSave, dragDroppedInstance, parentName))
        {
            return;
        }

        int flatListPosition = ResolveFlatListPositionForReorder(dropTarget, dragDroppedInstance);

        int droppedInstanceIndexBeforeMove = targetElementSave.Instances.IndexOf(dragDroppedInstance);

        if (droppedInstanceIndexBeforeMove != -1 && droppedInstanceIndexBeforeMove != flatListPosition)
        {
            targetElementSave.Instances.RemoveAt(droppedInstanceIndexBeforeMove);

            if (flatListPosition > droppedInstanceIndexBeforeMove)
            {
                flatListPosition -= 1;
            }
            if (flatListPosition > targetElementSave.Instances.Count)
            {
                flatListPosition = targetElementSave.Instances.Count;
            }
            if (flatListPosition < 0)
            {
                flatListPosition = 0;
            }

            targetElementSave.Instances.Insert(flatListPosition, dragDroppedInstance);

            _pluginManager.InstanceReordered(dragDroppedInstance);
        }

        // Since the Parent property can only be set in the default state, we will
        // set the Parent variable on that instead of the _selectedState.SelectedStateSave
        var stateToAssignOn = targetElementSave.DefaultState;

        // todo - this needs to request the lock for the particular element
        using var undoLock = _undoManager.RequestLock();

        var oldValue = stateToAssignOn.GetValue(variableName) as string;
        stateToAssignOn.SetValue(variableName, parentName, "string");

        _setVariableLogic.PropertyValueChanged("Parent", oldValue, dragDroppedInstance, targetElementSave.DefaultState);
        targetTreeNode?.Expand();
    }

    /// <summary>
    /// Computes the flat-list index inside <c>dropTarget.ParentElement.Instances</c>
    /// at which <paramref name="dragDroppedInstance"/> should land. For
    /// <see cref="DropPosition.Append"/> on an instance-targeted drop, this
    /// resolves to "after the last existing sibling-child"; for a direct
    /// element drop it resolves to "end of list" (which is the same thing
    /// since top-level instances are by definition the last in the flat list
    /// when the invariant holds). Self-inclusion no longer needs special
    /// handling because the caller's Remove/Insert math adjusts for the
    /// dragged instance being in the list.
    /// </summary>
    private int ResolveFlatListPositionForReorder(DropTarget dropTarget, InstanceSave dragDroppedInstance)
    {
        ElementSave element = dropTarget.ParentElement;
        switch (dropTarget.Position)
        {
            case DropPosition.Append:
                if (dropTarget.ParentInstance == null)
                {
                    return element.Instances.Count;
                }
                // Append-onto-Instance: place after the last existing child of
                // ParentInstance in the flat list. Excludes the dragged instance
                // so reorder-onto-own-parent doesn't anchor on itself.
                InstanceSave? lastSibling = FindLastSiblingOfParent(element, dropTarget.ParentInstance, dragDroppedInstance);
                return lastSibling != null
                    ? element.Instances.IndexOf(lastSibling) + 1
                    : element.Instances.IndexOf(dropTarget.ParentInstance) + 1;
            case DropPosition.InsertAt insertAt:
                return Math.Clamp(insertAt.Index, 0, element.Instances.Count);
            case DropPosition.BeforeSibling before:
            {
                int idx = element.Instances.IndexOf(before.Sibling);
                return idx < 0 ? element.Instances.Count : idx;
            }
            case DropPosition.AfterSibling after:
            {
                int idx = element.Instances.IndexOf(after.Sibling);
                return idx < 0 ? element.Instances.Count : idx + 1;
            }
            default:
                return element.Instances.Count;
        }
    }

    private static bool ParentVariableAlreadyMatches(ElementSave element, InstanceSave instance, string? expectedParentName)
    {
        var currentParent = element.DefaultState.GetVariableRecursive(instance.Name + ".Parent")?.Value as string;
        if (string.IsNullOrEmpty(expectedParentName))
        {
            return string.IsNullOrEmpty(currentParent);
        }
        // Exact match only. A bare "parent" and a "parent.defaultChild" slot
        // are different parentings (container root vs. the default child slot),
        // so they must NOT be treated as already-matching — otherwise the
        // early-out skips upgrading a freshly-added instance (created with the
        // bare parent name by AddInstance) to the parent's default child slot,
        // leaving it attached to the container root instead of the slot.
        return currentParent == expectedParentName;
    }

    private static InstanceSave? FindLastSiblingOfParent(ElementSave element, InstanceSave parentInstance, InstanceSave excludeInstance)
    {
        string parentName = parentInstance.Name;
        InstanceSave? last = null;
        foreach (var instance in element.Instances)
        {
            if (instance == excludeInstance)
            {
                continue;
            }
            var parentValue = element.DefaultState.GetVariableRecursive(instance.Name + ".Parent")?.Value;
            if (parentValue is string parentString &&
                (parentString == parentName || parentString.StartsWith(parentName + ".")))
            {
                last = instance;
            }
        }
        return last;
    }

    #endregion

    #region General Functions

    public bool ValidateNodeSorting(IEnumerable<ITreeNode> draggedNodes, ITreeNode? targetNode, DropTarget? dropTarget)
    {
        if (targetNode == null) return false;

        var toReturn = draggedNodes.All(item => ValidateDrop(item, targetNode));

        return toReturn;
    }

    bool ValidateDrop(ITreeNode draggedNode, ITreeNode targetTreeNode)
    {
        var draggedObject = draggedNode.Tag;
        var target = targetTreeNode.Tag;

        if (draggedObject == null)
        {
            return ValidateFolderDrop(draggedNode, targetTreeNode);
        }

        if(target is StandardElementSave)
        {
            // nothing can be dropped on standard elements:
            return false;
        }
        if(targetTreeNode.IsTopStandardElementTreeNode())
        {
            // nothing can be dropped on the standard elements folder:
            return false;
        }
        if(targetTreeNode.IsTopComponentContainerTreeNode())
        {
            if(draggedObject is StandardElementSave || draggedObject is BehaviorSave)
            {
                return false;
            }
        }
        if(draggedObject is ScreenSave && !targetTreeNode.IsScreensFolderTreeNode() && !targetTreeNode.IsTopScreenContainerTreeNode())
        {
            // screens cannot be added to anything else:
            return false;
        }

        ElementSave? targetElement = target as ElementSave;
        BehaviorSave? targetBehavior = target as BehaviorSave;



        if (target is InstanceSave targetInstance)
        {
            targetElement = ObjectFinder.Self.GetElementContainerOf(targetInstance);
            targetBehavior = ObjectFinder.Self.GetBehaviorContainerOf(targetInstance);
        }

        if(draggedObject is BehaviorSave behavior)
        {
            // behaviors cannot be added to screens currently:
            return targetElement is ComponentSave;
        }

        if(draggedObject is ElementSave draggedElement)
        {
            if(targetElement != null)
            {
                return _circularReferenceManager.CanTypeBeAddedToElement(
                    targetElement, draggedElement.Name);
            }
            else if(
                ( (targetTreeNode.IsComponentsFolderTreeNode() || targetTreeNode.IsTopComponentContainerTreeNode()) && draggedElement is ComponentSave) ||
                ( (targetTreeNode.IsScreensFolderTreeNode() || targetTreeNode.IsTopScreenContainerTreeNode()) && draggedElement is ScreenSave)
                )
            {
                var fullFolderPath = targetTreeNode.GetFullFilePath();

                var fullElementFilePath = draggedElement.GetFullPathXmlFile().GetDirectoryContainingThis();

                // If not equal, it was moved to a different folder, which is allowed:
                return fullFolderPath != fullElementFilePath;
            }
            else if(targetBehavior != null)
            {
                return draggedObject is StandardElementSave ||
                    draggedObject is ComponentSave;
            }
        }

        if(draggedObject is InstanceSave draggedInstance)
        {
            if (targetElement != null)
            {
                bool isSameElement = targetElement == draggedInstance.ParentContainer;

                if(isSameElement)
                {
                    return true;
                }
                else
                {
                    // do circular reference checks:
                    return _circularReferenceManager.CanTypeBeAddedToElement(
                        targetElement, draggedInstance.BaseType);
                }
            }
            else if(targetBehavior != null)
            {
                // Instances can always be added to behaviors:
                return true;
            }
        }

        return false;
    }

    private bool ValidateFolderDrop(ITreeNode draggedFolderNode, ITreeNode targetTreeNode)
    {
        bool isDraggedComponentsFolder = draggedFolderNode.IsComponentsFolderTreeNode();
        bool isDraggedScreensFolder = draggedFolderNode.IsScreensFolderTreeNode();

        // Only component and screen subfolders can be dragged
        if (!isDraggedComponentsFolder && !isDraggedScreensFolder)
        {
            return false;
        }

        // Target must be in the same folder hierarchy as the source
        bool isTargetInSameHierarchy;
        if (isDraggedComponentsFolder)
        {
            isTargetInSameHierarchy = targetTreeNode.IsComponentsFolderTreeNode() || targetTreeNode.IsTopComponentContainerTreeNode();
        }
        else if (isDraggedScreensFolder)
        {
            isTargetInSameHierarchy = targetTreeNode.IsScreensFolderTreeNode() || targetTreeNode.IsTopScreenContainerTreeNode();
        }
        else
        {
            return false;
        }

        if (!isTargetInSameHierarchy)
        {
            return false;
        }

        var draggedPath = draggedFolderNode.GetFullFilePath();
        var targetPath = targetTreeNode.GetFullFilePath();

        // Cannot drop onto itself
        if (draggedPath == targetPath)
        {
            return false;
        }

        // Cannot drop into a descendant of itself (circular move)
        if (targetPath.FullPath.StartsWith(draggedPath.FullPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Cannot drop into current parent (no-op)
        var parentPath = draggedFolderNode.Parent?.GetFullFilePath();
        if (parentPath != null && targetPath == parentPath)
        {
            return false;
        }

        return true;
    }

    public void OnNodeSortingDropped(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, DropTarget? dropTarget)
    {
        // Sort so that folders come first (they restructure the tree), then
        // InstanceSaves by source index. The direction depends on DropPosition:
        // BeforeSibling places each item *before* the anchor sibling, so the
        // first item processed ends up earlier in the list — process ascending
        // to preserve drag-order. Append/AfterSibling/InsertAt stack each item
        // *at* a fixed position, so processing descending preserves drag-order.
        bool ascendingForSiblings = dropTarget?.Position is DropPosition.BeforeSibling;

        var orderedByTag = draggedNodes.OrderBy(n => n.Tag == null ? 0 : 1);
        var sortedNodes = (ascendingForSiblings
            ? orderedByTag.ThenBy(InstanceSourceIndex)
            : orderedByTag.ThenByDescending(InstanceSourceIndex))
            .ToList();

        using var undoLock = _undoManager.RequestLock();

        foreach (var node in sortedNodes)
        {
            HandleDroppedItemOnTreeView(node, targetNode, dropTarget);
        }
    }

    private static int InstanceSourceIndex(ITreeNode node) =>
        node.Tag is InstanceSave instance
            ? instance.ParentContainer?.Instances.IndexOf(instance) ?? int.MinValue
            : int.MinValue;

    private void HandleDroppedFolder(ITreeNode draggedFolderNode, ITreeNode targetNode)
    {
        var oldFullPath = draggedFolderNode.GetFullFilePath();
        var targetFullPath = targetNode.GetFullFilePath();
        if (oldFullPath == null || targetFullPath == null)
        {
            return;
        }

        string folderName = draggedFolderNode.Text;
        string newFullPath = targetFullPath.FullPath + folderName + "\\";

        if (Directory.Exists(newFullPath))
        {
            _dialogService.ShowMessage(
                $"A folder named '{folderName}' already exists at the destination.");
            return;
        }

        string projectFolder =
            FileManager.GetDirectory(_projectManager.GumProjectSave.FullFileName);

        string subfolder;
        IEnumerable<ElementSave> elements;

        if (draggedFolderNode.IsComponentsFolderTreeNode())
        {
            subfolder = ElementReference.ComponentSubfolder;
            elements = _projectState.GumProjectSave.Components;
        }
        else if (draggedFolderNode.IsScreensFolderTreeNode())
        {
            subfolder = ElementReference.ScreenSubfolder;
            elements = _projectState.GumProjectSave.Screens;
        }
        else
        {
            return;
        }

        MoveElementsToFolder(elements, projectFolder + subfolder + "/",
            oldFullPath.FullPath, newFullPath);

        try
        {
            _fileCommands.MoveDirectory(oldFullPath.FullPath, newFullPath);
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage($"Could not move the folder. Additional information:\n{e}");
            return;
        }

        _guiCommands.RefreshElementTreeView();
    }

    private void MoveElementsToFolder(IEnumerable<ElementSave> elements,
        string root, string oldFullPath, string newFullPath)
    {
        string oldRel = FileManager.MakeRelative(oldFullPath, root, preserveCase: true)
            .Replace("\\", "/");
        string newRel = FileManager.MakeRelative(newFullPath, root, preserveCase: true)
            .Replace("\\", "/");

        foreach (var element in elements.ToArray())
        {
            if (element.Name.Replace("\\", "/")
                .StartsWith(oldRel, StringComparison.OrdinalIgnoreCase))
            {
                string oldName = element.Name;
                element.Name = (newRel + element.Name.Substring(oldRel.Length))
                    .Replace("\\", "/");
                _renameLogic.HandleRename(element, (InstanceSave?)null,
                    oldName, NameChangeAction.Move, askAboutRename: false);
            }
        }
    }

    public void OnFilesDroppedInTreeView(string[] files)
    {
        var targetTreeNode = _pluginManager.GetTreeNodeOver();

        if (files != null)
        {
            var isTargetRootScreenTreeNode = targetTreeNode.IsTopScreenContainerTreeNode();
            foreach (FilePath file in files)
            {
                if (file.Extension == GumProjectSave.ScreenExtension && isTargetRootScreenTreeNode)
                {
                    _importLogic.ImportScreen(file);
                }
            }
        }
    }

    private void HandleDroppedItemOnTreeView(ITreeNode draggedNode, ITreeNode treeNodeDroppedOn, DropTarget? dropTarget)
    {
        var draggedObject = draggedNode.Tag;
        Console.WriteLine($"Dropping{draggedObject} on {treeNodeDroppedOn}");
        if (treeNodeDroppedOn != null)
        {
            object targetTag = treeNodeDroppedOn.Tag;

            if (draggedObject == null)
            {
                HandleDroppedFolder(draggedNode, treeNodeDroppedOn);
            }
            else if (draggedObject is ElementSave)
            {
                HandleDroppedElementSave(draggedObject, treeNodeDroppedOn, targetTag, treeNodeDroppedOn, dropTarget);
            }
            else if (draggedObject is InstanceSave)
            {
                HandleDroppedInstance(draggedObject, treeNodeDroppedOn, dropTarget);
            }
            else if(draggedObject is BehaviorSave behaviorSave)
            {
                HandleDroppedBehavior(behaviorSave, treeNodeDroppedOn);
            }
        }
    }

    public void OnNodeObjectDroppedInWireframe(object draggedObject)
    {
        ElementSave? draggedAsElementSave = draggedObject as ElementSave;
        ElementSave? target = _wireframeObjectManager.ElementShowing;

        // Depending on how fast the user clicks the UI may think they dragged an instance rather than
        // an element, so let's protect against that with this null check.
        if (draggedAsElementSave != null && target is not null)
        {
            // Bundle the instance creation and the cursor-driven X/Y assignment into a
            // single undo entry. Without this lock, AddInstance records its own undo via
            // the InstanceAdd plugin event, and the subsequent X/Y SetValue calls leak
            // into whatever edit comes next. (issue #2658)
            using var undoLock = _undoManager.RequestLock();

            DropTarget appendTarget = new DropTarget(target, null, new DropPosition.Append());
            var newInstance = HandleDroppedElementInElement(draggedAsElementSave, target, null, appendTarget);

            float worldX, worldY;

            var position = _pluginManager.GetWorldCursorPosition(Cursor);

            worldX = position?.X ?? 0;
            worldY = position?.Y ?? 0;

            if(newInstance != null)
            {

                SetInstanceToPosition(worldX, worldY, newInstance);

                SaveAndRefresh();
            }
        }
    }

    /// <inheritdoc/>
    public string? GetFileDropBlockedReason()
    {
        if (_selectedState.SelectedStandardElement != null)
        {
            return $"a Standard element ({_selectedState.SelectedStandardElement.Name}) is selected — select a Screen or Component instead";
        }
        if (_selectedState.SelectedElement == null)
        {
            return "no Screen or Component is selected";
        }
        if (_selectedState.SelectedStateSave == null)
        {
            return "no state is selected";
        }
        return null;
    }

    /// <inheritdoc/>
    public DragAcceptDecision DecideWireframeDragEffect(bool hasFileDrop, bool hasNodes)
    {
        string? fileDropBlockedReason = GetFileDropBlockedReason();
        bool canDropFile = hasFileDrop && fileDropBlockedReason == null;

        if (canDropFile || hasNodes)
        {
            return new DragAcceptDecision(Accept: true, BlockedReason: null);
        }

        if (hasFileDrop && fileDropBlockedReason != null)
        {
            // The drop is being rejected (no Copy cursor). Surface why so an
            // otherwise-silent "drag+drop stopped working" is diagnosable (#3128).
            return new DragAcceptDecision(Accept: false, BlockedReason: fileDropBlockedReason);
        }

        return new DragAcceptDecision(Accept: false, BlockedReason: null);
    }

    private void SaveAndRefresh()
    {
        _fileCommands.TryAutoSaveCurrentElement();
        _guiCommands.RefreshVariables();
        _guiCommands.RefreshElementTreeView();

        _wireframeObjectManager.RefreshAll(true);
    }

    public void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance)
    {
        var component = _selectedState.SelectedComponent;

        float xToSet = worldX;
        float yToSet = worldY;

        float containerLeft = 0;
        float containerTop = 0;

        float containerWidth = _projectState.GumProjectSave.DefaultCanvasWidth;
        float containerHeight = _projectState.GumProjectSave.DefaultCanvasHeight;

        if (component != null)
        {
            var runtime = _wireframeObjectManager.GetRepresentation(component);
            containerLeft = runtime.GetAbsoluteLeft();
            containerTop = runtime.GetAbsoluteTop();

            containerWidth = runtime.Width;
            containerHeight = runtime.Height;

        }
        else
        {
            // leave default
        }

        var differenceX = worldX - containerLeft;



        var instanceXUnits = (PositionUnitType)_selectedState.SelectedStateSave.GetValueRecursive($"{instance.Name}.XUnits");
        var asGeneralXUnitType = UnitConverter.ConvertToGeneralUnit(instanceXUnits);
        xToSet = UnitConverter.Self.ConvertXPosition(differenceX, GeneralUnitType.PixelsFromSmall, asGeneralXUnitType, containerWidth);

        var differenceY = worldY - containerTop;
        var instanceYUnits = (PositionUnitType)_selectedState.SelectedStateSave.GetValueRecursive($"{instance.Name}.YUnits");
        var asGeneralYUnitType = UnitConverter.ConvertToGeneralUnit(instanceYUnits);
        yToSet = UnitConverter.Self.ConvertYPosition(differenceY, GeneralUnitType.PixelsFromSmall, asGeneralYUnitType, containerHeight);


        _selectedState.SelectedStateSave.SetValue(instance.Name + ".X", xToSet, "float");
        _selectedState.SelectedStateSave.SetValue(instance.Name + ".Y", yToSet, "float");
    }

    #endregion
}
