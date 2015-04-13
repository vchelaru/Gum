using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InputLibrary;
using Gum.DataTypes;
using System.Windows.Forms;
using Gum.Wireframe;
using ToolsUtilities;
using RenderingLibrary.Content;
using RenderingLibrary;
using Gum.Input;
using CommonFormsAndControls.Forms;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using Gum.ToolCommands;
using RenderingLibrary.Graphics;

namespace Gum.Managers
{
    public class DragDropManager
    {
        #region Fields

        static DragDropManager mSelf;

        object mDraggedItem;

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


        public void OnItemDrag(object item)
        {
            mDraggedItem = item;
        }

        public void Activity()
        {
            if (!Cursor.PrimaryDownIgnoringIsInWindow)
            {
                if (mDraggedItem != null)
                {
                    TreeNode draggedTreeNode = (TreeNode)mDraggedItem;
                    object draggedObject = draggedTreeNode.Tag;


                    mDraggedItem = null; // to prevent this from getting hit again if a message box is up


                    bool handled = false;
                    HandleDroppedItemInWireframe(draggedObject, out handled);

                    if (!handled)
                    {
                        TreeNode targetTreeNode = ElementTreeViewManager.Self.GetTreeNodeOver();
                        if (targetTreeNode != draggedTreeNode)
                        {
                            HandleDroppedItemOnTreeView(draggedObject, targetTreeNode);
                        }
                    }
                }
            }
        }

        private void HandleDroppedItemOnTreeView(object draggedObject, TreeNode treeNodeDroppedOn)
        {
            if (treeNodeDroppedOn != null)
            {
                object targetTag = treeNodeDroppedOn.Tag;

                if (draggedObject is ElementSave)
                {
                    ElementSave draggedAsElementSave = draggedObject as ElementSave;

                    // User dragged an element save - so they want to take something like a
                    // text object and make an instance in another element like a Screen
                    bool handled;

                    if (targetTag is ElementSave)
                    {
                        HandleDroppedElementInElement(draggedAsElementSave, targetTag as ElementSave, out handled);
                    }
                    else if (targetTag is InstanceSave)
                    {
                        // The user dropped it on an instance save, but he likely meant to drop
                        // it as an object under the current element.

                        InstanceSave targetInstance = targetTag as InstanceSave;


                        HandleDroppedElementInElement(draggedAsElementSave, targetInstance.ParentContainer, out handled);
                    }
                    else
                    {
                        MessageBox.Show("You must drop " + draggedAsElementSave.Name + " on either a Screen or an Component");
                    }
                }
                else if (draggedObject is InstanceSave)
                {
                    HandleDroppedInstance(draggedObject, targetTag);
                }
            }
        }

        private static void HandleDroppedInstance(object draggedObject, object targetTag)
        {
            InstanceSave draggedAsInstanceSave = draggedObject as InstanceSave;

            ElementSave targetElementSave = targetTag as ElementSave;
            if (targetElementSave == null && targetTag is InstanceSave)
            {
                targetElementSave = ((InstanceSave)targetTag).ParentContainer;
            }

            // We aren't going to allow drag+drop within the same element - that may cause
            // unexpected copy/paste if the user didn't mean to drag, and we may want to support
            // reordering.
            if (targetElementSave != null && targetElementSave != draggedAsInstanceSave.ParentContainer)
            {
                List<InstanceSave> instances = new List<InstanceSave>() { draggedAsInstanceSave };
                EditingManager.Self.PasteInstanceSaves(instances,
                    draggedAsInstanceSave.ParentContainer.DefaultState.Clone(),
                    targetElementSave);
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
                    var newInstance = HandleDroppedElementInElement(draggedAsElementSave, target, out handled);

                    float worldX, worldY;
                    Renderer.Self.Camera.ScreenToWorld(Cursor.X, Cursor.Y,
                                                       out worldX, out worldY);

                    SetInstanceToPosition(worldX, worldY, newInstance);

                    SaveAndRefresh();
                }
            }
        }

        private static InstanceSave HandleDroppedElementInElement(ElementSave draggedAsElementSave, ElementSave target, out bool handled)
        {
            InstanceSave newInstance = null;

            handled = false;
            if (target == null)
            {
                MessageBox.Show("No Screen or Component selected");
            }
            else if (target is StandardElementSave)
            {
                MessageBox.Show("Standard types can't contain objects");
            }
            else if (draggedAsElementSave is ScreenSave)
            {
                MessageBox.Show("Screens can't be dropped into other Screens or Components");
            }
            else
            {
                bool canAdd = true;

                if (draggedAsElementSave is ComponentSave && target is ComponentSave)
                {
                    ComponentSave targetAsComponentSave = target as ComponentSave;

                    if (!targetAsComponentSave.CanContainInstanceOfType(draggedAsElementSave.Name))
                    {
                        MessageBox.Show("Can't add instance of " + draggedAsElementSave.Name + " in " + targetAsComponentSave.Name);
                        canAdd = false;
                    }
                }
                if (target.IsSourceFileMissing)
                {
                    MessageBox.Show("The source file for " + target.Name + " is missing, so it cannot be edited");
                    canAdd = false;
                }

                if (canAdd)
                {
#if DEBUG
                    if(draggedAsElementSave == null)
                    {
                        throw new Exception("DraggedAsElementSave is null and it shouldn't be.  For vic - try to put this exception earlier to see what's up.");
                    }
#endif

                    string name = GetUniqueNameForNewInstance(draggedAsElementSave, target);

                    // First we want to re-select the target so that it is highlighted in the tree view and not
                    // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
                    ElementTreeViewManager.Self.Select(target);

                    newInstance = ElementTreeViewManager.Self.AddInstance(name, draggedAsElementSave.Name, target);
                    handled = true;
                }
            }

            return newInstance;
        }

        private static string GetUniqueNameForNewInstance(ElementSave elementSave, ElementSave element)
        {
#if DEBUG
            if (elementSave == null)
            {
                throw new ArgumentNullException("elementSave");
            }
#endif

            string name = elementSave.Name + "Instance";
            IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);

            return StringFunctions.MakeStringUnique(name, existingNames);
        }

        private bool CanDrop()
        {
            return SelectedState.Self.SelectedStandardElement == null &&    // Don't allow dropping on standard elements
                   SelectedState.Self.SelectedElement != null &&            // An element must be selected
                   SelectedState.Self.SelectedStateSave != null;            // A state must be selected
        }

        internal void HandleFileDragEnter(object sender, DragEventArgs e)
        {
            if (CanDrop() && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        internal void HandleFileDragDrop(object sender, DragEventArgs e)
        {
            if (!CanDrop())
                return;

            float worldX, worldY;
            Renderer.Self.Camera.ScreenToWorld(Cursor.X, Cursor.Y, out worldX, out worldY);

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // If only one file was dropped, see if we're over an instance that can take a file
            if (files.Length == 1)
            {
                if (!ValidExtension(files[0]))
                    return;

                InstanceSave instance = FindInstanceWithSourceFile(worldX, worldY);
                if (instance != null)
                {
                    string fileName = FileManager.MakeRelative(files[0], FileLocations.Self.ProjectFolder);

                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "What do you want to do with the file " + fileName;

                    mbmb.AddButton("Set source file on " + instance.Name, DialogResult.OK);
                    mbmb.AddButton("Add new Sprite", DialogResult.Yes);
                    mbmb.AddButton("Nothing", DialogResult.Cancel);

                    var result = mbmb.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".SourceFile", fileName, instance);
                        SaveAndRefresh();
                        return;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    // continue for DialogResult.Yes
                }
            }

            bool shouldUpdate = false;

            foreach (string file in files)
            {
                if (!ValidExtension(file))
                    continue;

                string fileName = FileManager.MakeRelative(file, FileLocations.Self.ProjectFolder);
                AddNewInstanceForDrop(fileName, worldX, worldY);
                shouldUpdate = true;
            }

            if (shouldUpdate)
                SaveAndRefresh();
        }

        private bool ValidExtension(string file)
        {
            string extension = FileManager.GetExtension(file);
            return LoaderManager.Self.ValidTextureExtensions.Contains(extension);
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

        private static void SaveAndRefresh()
        {
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            PropertyGridManager.Self.RefreshUI();
            GumCommands.Self.GuiCommands.RefreshElementTreeView();

            WireframeObjectManager.Self.RefreshAll(true);
        }

        private static void AddNewInstanceForDrop(string fileName, float worldX, float worldY)
        {
            string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

            IEnumerable<string> existingNames = SelectedState.Self.SelectedElement.Instances.Select(i => i.Name);
            nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

            InstanceSave instance =
                ElementCommands.Self.AddInstance(SelectedState.Self.SelectedElement, nameToAdd);
            instance.BaseType = "Sprite";

            SetInstanceToPosition(worldX, worldY, instance);

            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".SourceFile", fileName, instance);
        }

        private static void SetInstanceToPosition(float worldX, float worldY, InstanceSave instance)
        {
            float parentX = 0;
            float parentY = 0;
            if (SelectedState.Self.SelectedComponent != null)
            {
                parentX = (float)SelectedState.Self.SelectedStateSave.GetValueRecursive("X");
                parentY = (float)SelectedState.Self.SelectedStateSave.GetValueRecursive("Y");
            }

            // This thing may be left or center aligned so we should account for that

            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".X", worldX - parentX);
            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Y", worldY - parentY);
        }
    }
}
