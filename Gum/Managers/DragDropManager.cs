using System;
using System.Collections.Generic;
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

                if (draggedObject  is ElementSave)
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

            bool handled;

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

                HandleDroppedElementInElement(draggedAsElementSave, target, out handled);
            }

        }

        private static void HandleDroppedElementInElement(ElementSave draggedAsElementSave, ElementSave target, out bool handled)
        {
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
                    string name = GetUniqueNameForNewInstance(draggedAsElementSave, target);

                    // First we want to re-select the target so that it is highlighted in the tree view and not
                    // the object we dragged off.  This is so that plugins can properly use the SelectedElement.
                    ElementTreeViewManager.Self.Select(target);

                    ElementTreeViewManager.Self.AddInstance(name, draggedAsElementSave.Name, target);
                    handled = true;
                }
            }
        }

        private static string GetUniqueNameForNewInstance(ElementSave elementSave, ElementSave element)
        {
            string name = elementSave.Name + "Instance";

            // Gotta make this unique:
            List<string> existingNames = new List<string>();
            foreach (InstanceSave instance in element.Instances)
            {
                existingNames.Add(instance.Name);
            }
            name = StringFunctions.MakeStringUnique(name, existingNames);
            return name;
        }

        internal void HandleFileDragDrop(object sender, DragEventArgs e)
        {
            /////////////////////////////Early Out///////////////////////
            if (SelectedState.Self.SelectedStandardElement != null)
            {
                return;
            }
            ///////////////////////////End Early Out/////////////////////

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string firstFile = null;
            if(files.Length != 0)
            {
                firstFile = files[0];
            }

            if (!string.IsNullOrEmpty(firstFile))
            {
                string extension = FileManager.GetExtension(firstFile);

                if (LoaderManager.Self.ValidTextureExtensions.Contains(extension))
                {
                    HandleTextureFileDragDrop(firstFile);

                }
            }
        }

        private void HandleTextureFileDragDrop(string fileName)
        {
            // Make the filename relative:

            fileName = FileManager.MakeRelative(fileName,
                FileLocations.Self.ProjectFolder);



            // See if we're over representation that can take a file
            float worldX = SelectionManager.Self.Cursor.GetWorldX();
            float worldY = SelectionManager.Self.Cursor.GetWorldY();

            IPositionedSizedObject ipsoOver = 
                SelectionManager.Self.GetRepresentationAt(worldX, worldY, false);

            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();

            mbmb.MessageText = "What do you want to do with the file " + fileName;
            if (ipsoOver != null && ipsoOver.Tag is InstanceSave)
            {
                var baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(ipsoOver.Tag as InstanceSave);
                if (baseStandardElement.DefaultState.Variables.Any(item => item.Name == "SourceFile"))
                {
                    mbmb.AddButton("Set source file on " + ipsoOver.Name, DialogResult.OK);
                }
            }

            mbmb.AddButton("Add new Sprite", DialogResult.Yes);
            mbmb.AddButton("Nothing", DialogResult.Cancel);

            var result = mbmb.ShowDialog();
            bool shouldUpdate = false;

            if (result == DialogResult.OK)
            {
                InstanceSave instance = ipsoOver.Tag as InstanceSave;
                if (instance != null)
                {
                    // Need to make the file relative to the project:

                    SelectedState.Self.SelectedStateSave.SetValue(
                        instance.Name + ".SourceFile", fileName);
                    shouldUpdate = true;

                }
            }
            else if (result == DialogResult.Yes)
            {
                shouldUpdate = AddNewInstanceForDrop(fileName, worldX, worldY);
            }

            if (shouldUpdate)
            {
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                PropertyGridManager.Self.RefreshUI();
                ElementTreeViewManager.Self.RefreshUI();
                WireframeObjectManager.Self.RefreshAll(true);

            }

            int m = 3;

        }

        private static bool AddNewInstanceForDrop(string fileName, float worldX, float worldY)
        {
            bool shouldUpdate = true;
            string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));


            List<string> existingNames = new List<string>();
            foreach (InstanceSave existingInstance in SelectedState.Self.SelectedElement.Instances)
            {
                existingNames.Add(existingInstance.Name);
            }
            nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

            InstanceSave instance =
                ElementCommands.Self.AddInstance(SelectedState.Self.SelectedElement, nameToAdd);
            instance.BaseType = "Sprite";

            float parentX = 0;
            float parentY = 0;
            if (SelectedState.Self.SelectedComponent != null)
            {
                parentX = (float)SelectedState.Self.SelectedStateSave.GetValueRecursive("X");
                parentY = (float)SelectedState.Self.SelectedStateSave.GetValueRecursive("Y");
            }

            // This thing may be left or center aligned so we should account for that

            SelectedState.Self.SelectedStateSave.SetValue(
                    instance.Name + ".X", worldX - parentX);

            SelectedState.Self.SelectedStateSave.SetValue(
                    instance.Name + ".Y", worldY - parentY);



            SelectedState.Self.SelectedStateSave.SetValue(
                    instance.Name + ".SourceFile", fileName);


            shouldUpdate = true;
            return shouldUpdate;
        }

        internal void HandleFileDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }



        }
    }
}
