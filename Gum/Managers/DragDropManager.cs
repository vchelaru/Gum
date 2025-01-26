using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using System.Windows.Forms;
using Gum.Wireframe;
using ToolsUtilities;
using RenderingLibrary.Content;
using RenderingLibrary;
using CommonFormsAndControls.Forms;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using Gum.ToolCommands;
using RenderingLibrary.Graphics;
using Gum.PropertyGridHelpers;
using System.Drawing;
using Gum.Converters;
using Gum.Logic;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.DataTypes.Behaviors;
using Gum.Undo;

namespace Gum.Managers;

public class DragDropManager
{
    #region Fields

    static DragDropManager mSelf;

    object mDraggedItem;
    private readonly ISelectedState _selectedState;
    private readonly ElementCommands _elementCommands;

    #endregion

    #region Properties

    public InputLibrary.Cursor Cursor
    {
        get { return InputLibrary.Cursor.Self; }
    }

    public static DragDropManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new DragDropManager();
            }
            return mSelf;
        }
    }

    #endregion

    public DragDropManager()
    {
        _selectedState = SelectedState.Self;
        _elementCommands = ElementCommands.Self;
    }

    #region Drag+drop File (from windows explorer)

    internal void HandleFileDragDrop(object sender, DragEventArgs e)
    {
        if (!CanDrop())
            return;

        float worldX, worldY;
        Renderer.Self.Camera.ScreenToWorld(Cursor.X, Cursor.Y, out worldX, out worldY);
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        if(files == null)
        {
            return;
        }

        var handled = false;
        bool shouldUpdate = false;

        // If only one file was dropped, see if we're over an instance that can take a file
        if (files.Length == 1)
        {
            if (!IsValidExtensionForFileDrop(files[0]))
            {
                handled = true;
            }
        }

        if(!handled)
        {
            TryHandleFileDropOnInstance(worldX, worldY, files, ref handled, ref shouldUpdate);
        }

        if(!handled)
        {
            TryHandleFileDropOnComponent(worldX, worldY, files, ref handled, ref shouldUpdate);
        }


        if (!handled)
        {
            foreach (string file in files)
            {
                if (!IsValidExtensionForFileDrop(file))
                    continue;

                string fileName = FileManager.MakeRelative(file, FileLocations.Self.ProjectFolder);
                AddNewInstanceForDrop(fileName, worldX, worldY);
                shouldUpdate = true;
            }

        }
        if (shouldUpdate)
            SaveAndRefresh();
    }

    private void AddNewInstanceForDrop(string fileName, float worldX, float worldY)
    {
        string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

        var element = SelectedState.Self.SelectedElement;

        IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);
        nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

        InstanceSave instance =
            _elementCommands.AddInstance(element, nameToAdd);
        instance.BaseType = "Sprite";

        SetInstanceToPosition(worldX, worldY, instance);

        var variableName = instance.Name + ".SourceFile";

        var oldValue = SelectedState.Self.SelectedStateSave.GetValueOrDefault<string>(variableName);

        SelectedState.Self.SelectedStateSave.SetValue(variableName, fileName, instance);

        SetVariableLogic.Self.ReactToPropertyValueChanged("SourceFile", oldValue, element, instance, SelectedState.Self.SelectedStateSave, refresh: false);

    }

    private void TryHandleFileDropOnInstance(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        // This only supports drag+drop on an instance, but what if dropping on a component
        // which inherits from Sprite, or perhaps an instance that has an exposed file variable?
        // Not super high priority, but it's worth noting that this currently doesn't work...
        InstanceSave instance = FindInstanceWithSourceFile(worldX, worldY);
        if (instance != null)
        {
            string fileName = FileManager.MakeRelative(files[0], FileLocations.Self.ProjectFolder);

            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.StartPosition = FormStartPosition.Manual;

            mbmb.Location = new Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                 MainWindow.MousePosition.Y - mbmb.Height / 2);

            mbmb.MessageText = "What do you want to do with the file " + fileName;

            mbmb.AddButton("Set source file on " + instance.Name, DialogResult.OK);
            mbmb.AddButton("Add new Sprite", DialogResult.Yes);
            mbmb.AddButton("Nothing", DialogResult.Cancel);

            var result = mbmb.ShowDialog();

            if (result == DialogResult.OK)
            {
                var oldValue = SelectedState.Self.SelectedStateSave
                    .GetValueOrDefault<string>(instance.Name + ".SourceFile");

                SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".SourceFile", fileName, instance);
                ProjectState.Self.Selected.SelectedInstance = instance;
                SetVariableLogic.Self.PropertyValueChanged("SourceFile", oldValue, instance);

                shouldUpdate = true;
                handled = true;
            }
            else if (result == DialogResult.Cancel)
            {
                handled = true;

            }
            // continue for DialogResult.Yes
        }
    }

    private void TryHandleFileDropOnComponent(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement) { StateName = SelectedState.Self.SelectedStateSave.Name });

        // see if it's over the component:
        IPositionedSizedObject ipsoOver = SelectionManager.Self.GetRepresentationAt(worldX, worldY, false, elementStack);
        if(ipsoOver?.Tag is ComponentSave component && (component.BaseType == "Sprite" || component.BaseType == "NineSlice"))
        {
            string fileName = FileManager.MakeRelative(files[0], FileLocations.Self.ProjectFolder);

            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.StartPosition = FormStartPosition.Manual;

            mbmb.Location = new Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                 MainWindow.MousePosition.Y - mbmb.Height / 2);

            mbmb.MessageText = "What do you want to do with the file " + fileName;

            mbmb.AddButton("Set source file on " + component.Name, DialogResult.OK);
            mbmb.AddButton("Add new Sprite", DialogResult.Yes);
            mbmb.AddButton("Nothing", DialogResult.Cancel);


            var result = mbmb.ShowDialog();

            if (result == DialogResult.OK)
            {
                var oldValue = SelectedState.Self.SelectedStateSave
                    .GetValueOrDefault<string>("SourceFile");

                SelectedState.Self.SelectedStateSave.SetValue("SourceFile", fileName);
                ProjectState.Self.Selected.SelectedInstance = null;
                SetVariableLogic.Self.PropertyValueChanged("SourceFile", oldValue, SelectedState.Self.SelectedInstance);

                shouldUpdate = true;
                handled = true;
            }
            else if (result == DialogResult.Cancel)
            {
                handled = true;

            }

        }
    }

    private InstanceSave FindInstanceWithSourceFile(float worldX, float worldY)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement) { StateName = SelectedState.Self.SelectedStateSave.Name });

        IPositionedSizedObject ipsoOver = SelectionManager.Self.GetRepresentationAt(worldX, worldY, false, elementStack);

        if (ipsoOver != null && ipsoOver.Tag is InstanceSave)
        {
            var baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(ipsoOver.Tag as InstanceSave);

            if (baseStandardElement.DefaultState.Variables.Any(v => v.Name == "SourceFile"))
            {
                return ipsoOver.Tag as InstanceSave;
            }
        }

        return null;
    }

    private bool IsValidExtensionForFileDrop(string file)
    {
        string extension = FileManager.GetExtension(file);
        return LoaderManager.Self.ValidTextureExtensions.Contains(extension);
    }

    #endregion

    #region Drop Element (like components) on TreeView

    private void HandleDroppedElementSave(object draggedComponentOrElement, TreeNode treeNodeDroppedOn, object targetTag, TreeNode targetTreeNode)
    {
        ElementSave draggedAsElementSave = draggedComponentOrElement as ElementSave;

        // User dragged an element save - so they want to take something like a
        // text object and make an instance in another element like a Screen
        bool handled;

        if (targetTag is ElementSave)
        {
            HandleDroppedElementInElement(draggedAsElementSave, targetTag as ElementSave, null, out handled);
        }
        else if (targetTag is InstanceSave)
        {
            // The user dropped it on an instance save, but likely meant to drop
            // it as an object under the current element.

            InstanceSave targetInstance = targetTag as InstanceSave;

            // When a parent is set, we normally raise an event for that. This is a tricky situation because
            // we need to set the parent before adding the object.

            var newInstance = HandleDroppedElementInElement(draggedAsElementSave, targetInstance.ParentContainer, targetInstance, out handled);

            if(newInstance != null)
            {
                // Since the user dropped on another instance, let's try to parent it:
                HandleDroppingInstanceOnTarget(targetInstance, newInstance, targetInstance.ParentContainer, targetTreeNode);

                // HandleDroppingInstanceOnTarget internally calls 
                // WireframeObjectManager.Self.RefreshAll, but since
                // the Parent is set in HandleDroppedElementInElement,
                // then HandleDroppingInstanceOnTarget does not report the
                // parent as having changed. We need to still force a refresh
                // to make the parenting apply in the wireframe display.
                WireframeObjectManager.Self.RefreshAll(true, forceReloadTextures: false);
            }

        }
        else if (treeNodeDroppedOn.IsTopComponentContainerTreeNode())
        {
            HandleDroppedElementOnTopComponentTreeNode(draggedAsElementSave, out handled);

        }
        else if (draggedAsElementSave is ComponentSave && treeNodeDroppedOn.IsPartOfComponentsFolderStructure())
        {
            HandleDroppedElementOnFolder(draggedAsElementSave, treeNodeDroppedOn, out handled);
        }
        else if(draggedAsElementSave is ScreenSave && treeNodeDroppedOn.IsPartOfScreensFolderStructure())
        {
            HandleDroppedElementOnFolder(draggedAsElementSave, treeNodeDroppedOn, out handled);
        }
        else if(draggedAsElementSave is ScreenSave == false && targetTag is BehaviorSave targetBehavior)
        {
            HandleDroppedElementOnBehavior(draggedAsElementSave, targetBehavior);
        }
        else if(draggedAsElementSave is ScreenSave && targetTag is BehaviorSave)
        {
            MessageBox.Show("Screens cannot be added as required instances in behaviors");
        }
        else
        {
            MessageBox.Show("You must drop " + draggedAsElementSave.Name + " on either a Screen or an Component");
        }
    }

    private void HandleDroppedElementOnFolder(ElementSave draggedAsElementSave, TreeNode treeNodeDroppedOn, out bool handled)
    {
        if(draggedAsElementSave is StandardElementSave)
        {
            MessageBox.Show("Cannot move standard elements to different folders");
            handled = true;
        }
        else
        {
            var fullFolderPath = treeNodeDroppedOn.GetFullFilePath();

            var fullElementFilePath = draggedAsElementSave.GetFullPathXmlFile().GetDirectoryContainingThis();

            handled = false;

            if(fullFolderPath != fullElementFilePath)
            {
                var projectFolder = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                string nodeRelativeToProject = FileManager.MakeRelative(fullFolderPath.FullPath, projectFolder + draggedAsElementSave.Subfolder + "/", preserveCase:true)
                    .Replace("\\", "/");

                string oldName = draggedAsElementSave.Name;
                draggedAsElementSave.Name = nodeRelativeToProject + FileManager.RemovePath(draggedAsElementSave.Name);
                RenameLogic.HandleRename(draggedAsElementSave, (InstanceSave)null,  oldName, NameChangeAction.Move);

                handled = true;
            }

        }

    }

    private void HandleDroppedElementOnTopComponentTreeNode(ElementSave draggedAsElementSave, out bool handled)
    {
        handled = false;
        string name = draggedAsElementSave.Name;

        string currentDirectory = FileManager.GetDirectory(draggedAsElementSave.Name);

        if(!string.IsNullOrEmpty(currentDirectory))
        {
            // It's in a directory, we're going to move it out
            draggedAsElementSave.Name = FileManager.RemovePath(name);
            RenameLogic.HandleRename(draggedAsElementSave, (InstanceSave)null, name, NameChangeAction.Move);

            handled = true;
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
            MessageBox.Show(errorMessage);
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
            ElementTreeViewManager.Self.Select(behavior);

            newInstance = _elementCommands.AddInstance(behavior, name, draggedElement.Name);
            //handled = true;
        }

        return newInstance;
    }

    private InstanceSave HandleDroppedElementInElement(ElementSave draggedAsElementSave, ElementSave target, InstanceSave parentInstance, out bool handled)
    {
        InstanceSave newInstance = null;

        string errorMessage = null;

        handled = false;

        errorMessage = GetDropElementErrorMessage(draggedAsElementSave, target, errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            MessageBox.Show(errorMessage);
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
            ElementTreeViewManager.Self.Select(target);

            newInstance = _elementCommands.AddInstance(target, name, draggedAsElementSave.Name, parentInstance?.Name);
            handled = true;
        }

        return newInstance;
    }

    private string GetDropElementErrorMessage(ElementSave draggedAsElementSave, ElementSave target, string errorMessage)
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

            if (draggedAsElementSave is ComponentSave && target is ComponentSave)
            {
                ComponentSave targetAsComponentSave = target as ComponentSave;

                if (!targetAsComponentSave.CanContainInstanceOfType(draggedAsElementSave.Name))
                {
                    errorMessage = "Can't add instance of " + draggedAsElementSave.Name + " in " + targetAsComponentSave.Name;
                }
            }
        }


        if (errorMessage == null && target.IsSourceFileMissing)
        {
            errorMessage = "The source file for " + target.Name + " is missing, so it cannot be edited";
        }

        if(errorMessage == null && target == GumState.Self.SelectedState.SelectedElement)
        {
            if(GumState.Self.SelectedState.SelectedStateSave != GumState.Self.SelectedState.SelectedElement.DefaultState)
            {
                errorMessage = $"Cannot add instances to " +
                    $"{GumState.Self.SelectedState.SelectedElement} while the {GumState.Self.SelectedState.SelectedStateSave} " +
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

    private void HandleDroppedBehavior(BehaviorSave behavior, TreeNode treeNodeDroppedOn)
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
        
        using var undoLock = UndoManager.Self.RequestLock();


        _elementCommands.AddBehaviorTo(behavior, targetComponent);

        if(targetComponent == SelectedState.Self.SelectedComponent)
        {
            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            GumCommands.Self.GuiCommands.BroadcastRefreshBehaviorView();
        }
    }

    #endregion

    #region Drop Instance on TreeNode

    private void HandleDroppedInstance(object draggedObject, TreeNode targetTreeNode)
    {
        object targetObject = targetTreeNode.Tag;

        InstanceSave draggedAsInstanceSave = draggedObject as InstanceSave;

        ElementSave targetElementSave = targetObject as ElementSave;
        InstanceSave targetInstanceSave = targetObject as InstanceSave;
        if (targetElementSave == null && targetInstanceSave != null)
        {
            targetElementSave = targetInstanceSave.ParentContainer;
        }


        bool isSameElement = draggedAsInstanceSave != null && targetElementSave == draggedAsInstanceSave.ParentContainer;

        if (targetElementSave != null)
        {
            if (isSameElement)
            {
                HandleDroppingInstanceOnTarget(targetObject, draggedAsInstanceSave, targetElementSave, targetTreeNode);

            }
            else
            {
                List<InstanceSave> instances = new List<InstanceSave>() { draggedAsInstanceSave };
                List<StateSave> stateWithVariablesForOriginalInstance = new List<StateSave>
                {
                    draggedAsInstanceSave.ParentContainer?.DefaultState.Clone() ?? new StateSave()
                };
                    
                CopyPasteLogic.Self.PasteInstanceSaves(instances,
                    stateWithVariablesForOriginalInstance,
                    targetElementSave, targetInstanceSave);
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
        GumCommands.Self.GuiCommands.RefreshElementTreeView();
        GumCommands.Self.FileCommands.TryAutoSaveBehavior(asBehaviorSave);

    }

    private void HandleDroppingInstanceOnTarget(object targetObject, InstanceSave dragDroppedInstance, ElementSave targetElementSave, TreeNode targetTreeNode)
    {
        var instanceDefinedByBase = dragDroppedInstance.DefinedByBase;

        if(instanceDefinedByBase)
        {
            GumCommands.Self.GuiCommands.ShowMessage($"{dragDroppedInstance.Name} cannot be added as a child of {targetObject} because it is defined in a base element");
        }
        else
        {
            string parentName;
            string variableName = dragDroppedInstance.Name + ".Parent";
            if (targetObject is InstanceSave targetInstance)
            {
                // setting the parent:
                parentName = targetInstance.Name;
                string defaultChild = ObjectFinder.Self.GetDefaultChildName(targetInstance, SelectedState.Self.SelectedStateSave);

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
            // Since the Parent property can only be set in the default state, we will
            // set the Parent variable on that instead of the SelectedState.Self.SelectedStateSave
            var stateToAssignOn = targetElementSave.DefaultState;

            using var undoLock = UndoManager.Self.RequestLock();

            var oldValue = stateToAssignOn.GetValue(variableName) as string;
            stateToAssignOn.SetValue(variableName, parentName, "string");
            

            SetVariableLogic.Self.PropertyValueChanged("Parent", oldValue, dragDroppedInstance);
            targetTreeNode?.Expand();
        }
    }

    internal void ClearDraggedItem()
    {
        mDraggedItem = null;
    }

    internal void HandleKeyPress(KeyPressEventArgs e)
    {
        int m = 3;
    }

    #endregion

    #region General Functions

    internal void HandleDragDropEvent(object sender, DragEventArgs e)
    {
        List<TreeNode> treeNodesToDrop = GetTreeNodesToDrop();
        mDraggedItem = null;
        TreeNode targetTreeNode = ElementTreeViewManager.Self.GetTreeNodeOver();
        foreach(var draggedTreeNode in treeNodesToDrop )
        {
            object draggedObject = draggedTreeNode.Tag;

            if (targetTreeNode != draggedTreeNode)
            {
                HandleDroppedItemOnTreeView(draggedObject, targetTreeNode);
            }
        }

        string[] files = (string[])e.Data?.GetData(DataFormats.FileDrop);

        if(files != null)
        {
            var isTargetRootScreenTreeNode = targetTreeNode == ElementTreeViewManager.Self.RootScreensTreeNode;
            foreach(FilePath file in files)
            {
                if(file.Extension == GumProjectSave.ScreenExtension && isTargetRootScreenTreeNode)
                {
                    ImportLogic.ImportScreen(file);
                }
            }
        }
    }

    public void OnItemDrag(object item)
    {
        mDraggedItem = item;
    }

    public void Activity()
    {
        if (mDraggedItem != null)
        {
            // May 11, 2021
            // I thought that
            // we had to manually
            // set the cursor here
            // if drag+dropping a tree
            // node. It turns out that this
            // is handled by HandleFileDragEnter
            //if (InputLibrary.Cursor.Self.IsInWindow)
            //{
            //    InputLibrary.Cursor.Self.SetWinformsCursor(
            //        System.Windows.Forms.Cursors.Arrow);

            //}


            if (!Cursor.PrimaryDownIgnoringIsInWindow)
            {
                List<TreeNode> treeNodesToDrop = GetTreeNodesToDrop();

                foreach (var draggedTreeNode in treeNodesToDrop)
                {
                    object draggedObject = draggedTreeNode.Tag;

                    HandleDroppedItemInWireframe(draggedObject, out bool handled);

                    if(handled)
                    {
                        mDraggedItem = null;
                    }
                }
            }
        }
    }

    private List<TreeNode> GetTreeNodesToDrop()
    {
        List<TreeNode> treeNodesToDrop = new List<TreeNode>();

        if(mDraggedItem != null && ((TreeNode)mDraggedItem).Tag != null)
        {
            treeNodesToDrop.Add((TreeNode)mDraggedItem);
        }

        // The selected nodes should contain the dragged item, but I don't know for 100% certain.
        // If not, then we'll just use the dragged item. If it does, then we'll also add all other
        // selected items:
        if (SelectedState.Self.SelectedTreeNodes.Contains(mDraggedItem))
        {
            var whatToAdd = SelectedState.Self.SelectedTreeNodes.Where(item => item != mDraggedItem && item != null && item.Tag != null);
            treeNodesToDrop.AddRange(whatToAdd);
        }

        return treeNodesToDrop;
    }

    private void HandleDroppedItemOnTreeView(object draggedObject, TreeNode treeNodeDroppedOn)
    {
        Console.WriteLine($"Dropping{draggedObject} on {treeNodeDroppedOn}");
        if (treeNodeDroppedOn != null)
        {
            object targetTag = treeNodeDroppedOn.Tag;

            if (draggedObject is ElementSave)
            {
                HandleDroppedElementSave(draggedObject, treeNodeDroppedOn, targetTag, treeNodeDroppedOn);
            }
            else if (draggedObject is InstanceSave)
            {
                HandleDroppedInstance(draggedObject, treeNodeDroppedOn);
            }
            else if(draggedObject is BehaviorSave behaviorSave)
            {
                HandleDroppedBehavior(behaviorSave, treeNodeDroppedOn);
            }
        }
    }

    private void HandleDroppedItemInWireframe(object draggedObject, out bool handled)
    {
        handled = false;

        if (Cursor.IsInWindow)
        {   
            ElementSave draggedAsElementSave = draggedObject as ElementSave;                    
            ElementSave target = Wireframe.WireframeObjectManager.Self.ElementShowing;

            // Depending on how fast the user clicks the UI may think they dragged an instance rather than 
            // an element, so let's protect against that with this null check.
            if (draggedAsElementSave != null)
            {
                var newInstance = HandleDroppedElementInElement(draggedAsElementSave, target, null, out handled);

                float worldX, worldY;
                Renderer.Self.Camera.ScreenToWorld(Cursor.X, Cursor.Y,
                                                   out worldX, out worldY);
                if(newInstance != null)
                {

                    SetInstanceToPosition(worldX, worldY, newInstance);

                    SaveAndRefresh();
                }
                mDraggedItem = null;
            }
        }
    }

    private bool CanDrop()
    {
        return SelectedState.Self.SelectedStandardElement == null &&    // Don't allow dropping on standard elements
               SelectedState.Self.SelectedElement != null &&            // An element must be selected
               SelectedState.Self.SelectedStateSave != null;            // A state must be selected
    }

    internal void HandleFileDragEnter(object sender, DragEventArgs e)
    {
        UpdateEffectsForDragging(e);
    }

    internal void HandleDragOver(object sender, DragEventArgs e)
    {
        UpdateEffectsForDragging(e);
    }

    private void UpdateEffectsForDragging(DragEventArgs e)
    {
        if (CanDrop())
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else if (mDraggedItem != null)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
    }

    private void SaveAndRefresh()
    {
        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        GumCommands.Self.GuiCommands.RefreshVariables();
        GumCommands.Self.GuiCommands.RefreshElementTreeView();

        WireframeObjectManager.Self.RefreshAll(true);
    }

    internal void HandleKeyDown(System.Windows.Forms.KeyEventArgs e)
    {
        if(e.KeyCode == Keys.Escape)
        {
            mDraggedItem = null;
        }
    }

    private void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance)
    {
        var component = SelectedState.Self.SelectedComponent;

        float xToSet = worldX;
        float yToSet = worldY;

        float containerLeft = 0;
        float containerTop = 0;

        float containerWidth = ProjectState.Self.GumProjectSave.DefaultCanvasWidth;
        float containerHeight = ProjectState.Self.GumProjectSave.DefaultCanvasHeight;

        if (component != null)
        {
            var runtime = Wireframe.WireframeObjectManager.Self.GetRepresentation(component);
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



        var instanceXUnits = (PositionUnitType)SelectedState.Self.SelectedStateSave.GetValueRecursive($"{instance.Name}.XUnits");
        var asGeneralXUnitType = UnitConverter.ConvertToGeneralUnit(instanceXUnits);
        xToSet = UnitConverter.Self.ConvertXPosition(differenceX, GeneralUnitType.PixelsFromSmall, asGeneralXUnitType, containerWidth);

        var differenceY = worldY - containerTop;
        var instanceYUnits = (PositionUnitType)SelectedState.Self.SelectedStateSave.GetValueRecursive($"{instance.Name}.YUnits");
        var asGeneralYUnitType = UnitConverter.ConvertToGeneralUnit(instanceYUnits);
        yToSet = UnitConverter.Self.ConvertYPosition(differenceY, GeneralUnitType.PixelsFromSmall, asGeneralYUnitType, containerHeight);


        SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".X", xToSet);
        SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Y", yToSet);
    }

    #endregion


}
