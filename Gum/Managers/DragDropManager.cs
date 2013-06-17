using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputLibrary;
using Gum.DataTypes;
using System.Windows.Forms;
using Gum.Wireframe;
using ToolsUtilities;

namespace Gum.Managers
{
    public class DragDropManager
    {
        static DragDropManager mSelf;

        object mDraggedItem;

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
                EditingManager.Self.PasteInstanceSave(draggedAsInstanceSave,
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
    }
}
