using CommonFormsAndControls;
using CommonFormsAndControls.Forms;
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
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows.Forms;
using System.Windows.Navigation;
using Gum.Extensions;
using ToolsUtilities;

namespace Gum.Managers;


public interface ITreeNode
{
    object Tag { get; }
    FilePath GetFullFilePath();
    ITreeNode? Parent { get; }
    string Text { get; }
    string FullPath { get; }

    void Expand();
}

public class DragDropManager
{
    #region Fields

    static DragDropManager mSelf;

    private readonly CircularReferenceManager _circularReferenceManager;
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly IUndoManager _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly SetVariableLogic _setVariableLogic;
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly ImportLogic _importLogic;
    private readonly WireframeObjectManager _wireframeObjectManager;
    private readonly PluginManager _pluginManager;
    private readonly ReorderLogic _reorderLogic;

    #endregion

    #region Properties

    public InputLibrary.Cursor Cursor
    {
        get { return InputLibrary.Cursor.Self; }
    }


    #endregion

    #region Constructor

    public DragDropManager(CircularReferenceManager circularReferenceManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IRenameLogic renameLogic,
        IUndoManager undoManager,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        SetVariableLogic setVariableLogic, 
        CopyPasteLogic copyPasteLogic,
        ImportLogic importLogic,
        WireframeObjectManager wireframeObjectManager,
        PluginManager pluginManager,
        ReorderLogic reorderLogic)
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

    public bool IsValidExtensionForFileDrop(string file)
    {
        string extension = FileManager.GetExtension(file);
        return ValidTextureExtensions.Contains(extension);
    }

    #endregion

    #region Drop Element (like components) on TreeView

    private void HandleDroppedElementSave(object draggedComponentOrElement, ITreeNode treeNodeDroppedOn, object targetTag, ITreeNode targetTreeNode, int index)
    {
        ElementSave draggedAsElementSave = draggedComponentOrElement as ElementSave;

        // User dragged an element save - so they want to take something like a
        // text object and make an instance in another element like a Screen

        if (targetTag is ElementSave)
        {
            HandleDroppedElementInElement(draggedAsElementSave, targetTag as ElementSave, null, index);
        }
        else if (targetTag is InstanceSave)
        {
            // The user dropped it on an instance save, but likely meant to drop
            // it as an object under the current element.

            InstanceSave targetInstance = targetTag as InstanceSave;

            // When a parent is set, we normally raise an event for that. This is a tricky situation because
            // we need to set the parent before adding the object.

            var newInstance = HandleDroppedElementInElement(draggedAsElementSave, targetInstance.ParentContainer, targetInstance, index);

            if(newInstance != null)
            {
                // Since the user dropped on another instance, let's try to parent it:
                HandleDroppingInstanceOnTarget(targetInstance, newInstance, targetInstance.ParentContainer, targetTreeNode, index);

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
                var projectFolder = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

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

            string name = GetUniqueNameForNewInstance(draggedElement, behavior);

            // First we want to re-select the target so that it is highlighted in the tree view and not
            // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
            _selectedState.SelectedBehavior = behavior;

            newInstance = _elementCommands.AddInstance(behavior, name, draggedElement.Name);
            //handled = true;
        }

        return newInstance;
    }

    private InstanceSave HandleDroppedElementInElement(ElementSave draggedAsElementSave, ElementSave target, InstanceSave parentInstance, int index)
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

            string name = GetUniqueNameForNewInstance(draggedAsElementSave, target);

            // First we want to re-select the target so that it is highlighted in the tree view and not
            // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
            _selectedState.SelectedElement = target;

            newInstance = _elementCommands.AddInstance(target, name, draggedAsElementSave.Name, parentInstance?.Name, index);
        }

        return newInstance;
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

    private string GetUniqueNameForNewInstance(ElementSave elementSaveForNewInstance, ElementSave element)
    {
#if DEBUG
        if (elementSaveForNewInstance == null)
        {
            throw new ArgumentNullException("elementSave");
        }
#endif
        // remove the path - we dont want folders to be part of the name
        string name = FileManager.RemovePath( elementSaveForNewInstance.Name ) + "Instance";
        IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);

        return StringFunctions.MakeStringUnique(name, existingNames);
    }


    private string GetUniqueNameForNewInstance(ElementSave elementSaveForNewInstance, BehaviorSave container)
    {
#if DEBUG
        if (elementSaveForNewInstance == null)
        {
            throw new ArgumentNullException(nameof(elementSaveForNewInstance));
        }
#endif
        // remove the path - we dont want folders to be part of the name
        string name = FileManager.RemovePath(elementSaveForNewInstance.Name) + "Instance";
        IEnumerable<string> existingNames = container.RequiredInstances.Select(i => i.Name);

        return StringFunctions.MakeStringUnique(name, existingNames);
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

    private void HandleDroppedInstance(object draggedObject, ITreeNode targetTreeNode, int index)
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
                HandleDroppingInstanceOnTarget(targetObject, draggedAsInstanceSave, targetElementSave, targetTreeNode, index);

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
                if(firstInstance != null && targetElementSave.Instances.IndexOf(firstInstance) != index)
                {
                    targetElementSave.Instances.Remove(firstInstance);
                    targetElementSave.Instances.Insert(index, firstInstance);

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
        var behaviorInstanceSave = new BehaviorInstanceSave();
        behaviorInstanceSave.Name = draggedAsInstanceSave.Name;
        behaviorInstanceSave.BaseType = draggedAsInstanceSave.BaseType;
        asBehaviorSave.RequiredInstances.Add(behaviorInstanceSave);
        _guiCommands.RefreshElementTreeView();
        _fileCommands.TryAutoSaveBehavior(asBehaviorSave);

    }

    private void HandleDroppingInstanceOnTarget(object targetObject, InstanceSave dragDroppedInstance, ElementSave targetElementSave, ITreeNode targetTreeNode, int index)
    {
        var instanceDefinedByBase = dragDroppedInstance.DefinedByBase;

        if(instanceDefinedByBase)
        {
            _dialogService.ShowMessage($"{dragDroppedInstance.Name} cannot be added as a child of {targetObject} because it is defined in a base element");
        }
        else
        {
            string parentName;
            string variableName = dragDroppedInstance.Name + ".Parent";

            var siblings = new List<InstanceSave>();

            if (targetObject is InstanceSave targetInstance)
            {
                // setting the parent:
                parentName = targetInstance.Name;
                string defaultChild = ObjectFinder.Self.GetDefaultChildName(targetInstance, _selectedState.SelectedStateSave);

                if (!string.IsNullOrEmpty(defaultChild))
                {
                    parentName += "." + defaultChild;
                }

                foreach(var instance in targetElementSave.Instances)
                {
                    var instanceParent = targetElementSave.DefaultState.GetVariableRecursive(instance.Name + ".Parent")?.Value;

                    if (instanceParent is string instanceParentAsString && 
                        (instanceParentAsString == parentName || instanceParentAsString?.StartsWith($"{parentName}.") == true))
                    {
                        siblings.Add(instance);
                    }
                }
            }
            else
            {
                // drag+drop on the container, so detach:
                parentName = null;
                foreach (var instance in targetElementSave.Instances)
                {
                    var instanceParent = targetElementSave.DefaultState.GetVariableRecursive(instance.Name + ".Parent")?.Value;

                    if (instanceParent == null || (instanceParent is string instanceParentAsString && string.IsNullOrWhiteSpace(instanceParentAsString)))
                    {
                        siblings.Add(instance);
                    }
                }
            }

            // in case there's some error here:
            var siblingAfter = index > 0 && index - 1 < siblings.Count ? siblings[index - 1] : null;

            int indexToAddAt = 0;

            if(siblingAfter != null)
            {
                indexToAddAt = targetElementSave.Instances.IndexOf(siblingAfter) + 1;
            }

            var droppedInstanceIndexBeforeMove = targetElementSave.Instances.IndexOf(dragDroppedInstance);

            if(droppedInstanceIndexBeforeMove != -1 && droppedInstanceIndexBeforeMove != indexToAddAt)
            {
                if(indexToAddAt > droppedInstanceIndexBeforeMove)
                {
                    indexToAddAt -= 1;
                }
                targetElementSave.Instances.RemoveAt(droppedInstanceIndexBeforeMove);
                targetElementSave.Instances.Insert(indexToAddAt, dragDroppedInstance);

                _pluginManager.InstanceReordered(dragDroppedInstance);
            }

            // Since the Parent property can only be set in the default state, we will
            // set the Parent variable on that instead of the _selectedState.SelectedStateSave
            var stateToAssignOn = targetElementSave.DefaultState;

            // todo - this needs to request the lock for the particular element
            using var undoLock = _undoManager.RequestLock();

            var oldValue = stateToAssignOn.GetValue(variableName) as string;
            stateToAssignOn.SetValue(variableName, parentName, "string");
            

            _setVariableLogic.PropertyValueChanged("Parent", oldValue, dragDroppedInstance, targetElementSave?.DefaultState);
            targetTreeNode?.Expand();
        }
    }

    internal void HandleKeyPress(KeyPressEventArgs e)
    {
        int m = 3;
    }

    #endregion

    #region General Functions

    internal bool ValidateNodeSorting(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, int index)
    {
        if (targetNode == null) return false;

        var toReturn = draggedNodes.All(item => ValidateDrop(item.Tag, targetNode));

        return toReturn;
    }

    bool ValidateDrop(object draggedObject, ITreeNode targetTreeNode)
    {
        var target = targetTreeNode.Tag;

        if (draggedObject == null) return false;

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

    public void OnNodeSortingDropped(IEnumerable<ITreeNode> draggedNodes, ITreeNode targetNode, int index)
    {
        IEnumerable<object> tags = draggedNodes
            .Where(n => n.Tag != null)
            .Select(n => n.Tag);

        foreach (object draggedObject in tags)
        {
            HandleDroppedItemOnTreeView(draggedObject, targetNode, index);
        }
    }

    internal void OnFilesDroppedInTreeView(string[] files)
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

    private void HandleDroppedItemOnTreeView(object draggedObject, ITreeNode treeNodeDroppedOn, int index)
    {
        Console.WriteLine($"Dropping{draggedObject} on {treeNodeDroppedOn}");
        if (treeNodeDroppedOn != null)
        {
            object targetTag = treeNodeDroppedOn.Tag;

            if (draggedObject is ElementSave)
            {
                HandleDroppedElementSave(draggedObject, treeNodeDroppedOn, targetTag, treeNodeDroppedOn, index);
            }
            else if (draggedObject is InstanceSave)
            {
                HandleDroppedInstance(draggedObject, treeNodeDroppedOn, index);
            }
            else if(draggedObject is BehaviorSave behaviorSave)
            {
                HandleDroppedBehavior(behaviorSave, treeNodeDroppedOn);
            }
        }
    }

    public void OnNodeObjectDroppedInWireframe(object draggedObject)
    {
        ElementSave draggedAsElementSave = draggedObject as ElementSave;                    
        ElementSave? target = _wireframeObjectManager.ElementShowing;

        // Depending on how fast the user clicks the UI may think they dragged an instance rather than 
        // an element, so let's protect against that with this null check.
        if (draggedAsElementSave != null && target is not null)
        {
            var index = target.Instances.Count;
            var newInstance = HandleDroppedElementInElement(draggedAsElementSave, target, null, index);

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

    public void OnWireframeDragEnter(object sender, DragEventArgs e)
    {
        var canDropFile =
            _selectedState.SelectedStandardElement == null &&    // Don't allow dropping on standard elements
            _selectedState.SelectedElement != null &&            // An element must be selected
            _selectedState.SelectedStateSave != null &&
            e.Data.GetDataPresent(DataFormats.FileDrop);            // A state must be selected

        var isNodes = e.HasData<List<TreeNode>>() || e.HasData<TreeNode>();

        if (canDropFile || isNodes)
        {
            e.Effect = DragDropEffects.Copy;
        }
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

        float containerWidth = ProjectState.Self.GumProjectSave.DefaultCanvasWidth;
        float containerHeight = ProjectState.Self.GumProjectSave.DefaultCanvasHeight;

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
