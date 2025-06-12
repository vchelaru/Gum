﻿using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumCommon;

namespace Gum.Logic
{
    public class ReorderLogic : Singleton<ReorderLogic>
    {
        private ISelectedState _selectedState;

        public ReorderLogic()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }
        
        public void MoveSelectedInstanceForward()
        {
            var instance = _selectedState.SelectedInstance;
            var element = _selectedState.SelectedElement;

            if (instance != null)
            {
                var siblingInstances = instance.GetSiblingsIncludingThis();
                var thisIndex = siblingInstances.IndexOf(instance);
                bool isLast = thisIndex == siblingInstances.Count - 1;

                if (!isLast)
                {
                    using (UndoManager.Self.RequestLock())
                    {

                        // remove it before getting the new index, or else the removal could impact the
                        // index.
                        element.Instances.Remove(instance);
                        var nextSibling = siblingInstances[thisIndex + 1];

                        var nextSiblingIndexInContainer = element.Instances.IndexOf(nextSibling);

                        element.Instances.Insert(nextSiblingIndexInContainer + 1, instance);
                        RefreshInResponseToReorder(instance);
                    }
                }
            }
        }

        public void MoveSelectedInstanceBackward()
        {
            var instance = _selectedState.SelectedInstance;
            var element = _selectedState.SelectedElement;

            if (instance != null)
            {
                // remove it before getting the new index, or else the removal could impact the
                // index.
                var siblingInstances = instance.GetSiblingsIncludingThis();
                var thisIndex = siblingInstances.IndexOf(instance);
                bool isFirst = thisIndex == 0;

                if (!isFirst)
                {
                    using (UndoManager.Self.RequestLock())
                    {

                        element.Instances.Remove(instance);
                        var previousSibling = siblingInstances[thisIndex - 1];

                        var previousSiblingIndexInContainer = element.Instances.IndexOf(previousSibling);

                        element.Instances.Insert(previousSiblingIndexInContainer, instance);
                        RefreshInResponseToReorder(instance);
                    }
                }
            }
        }

        public void MoveSelectedInstanceToFront()
        {
            InstanceSave instance = _selectedState.SelectedInstance;
            ElementSave element = _selectedState.SelectedElement;

            if (instance != null)
            {
                using (UndoManager.Self.RequestLock())
                {

                    // to bring to back, we're going to remove, then add (at the end)
                    element.Instances.Remove(instance);
                    element.Instances.Add(instance);

                    RefreshInResponseToReorder(instance);
                }
            }
        }

        public void MoveSelectedInstanceToBack()
        {
            InstanceSave instance = _selectedState.SelectedInstance;
            ElementSave element = _selectedState.SelectedElement;

            if (instance != null)
            {
                using (UndoManager.Self.RequestLock())
                {

                    // to bring to back, we're going to remove, then insert at index 0
                    element.Instances.Remove(instance);
                    element.Instances.Insert(0, instance);

                    RefreshInResponseToReorder(instance);
                }
            }
        }

        public void MoveSelectedInstanceInFrontOf(InstanceSave whatToMoveInFrontOf)
        {
            var element = _selectedState.SelectedElement;
            var whatToInsert = _selectedState.SelectedInstance;
            if (whatToInsert != null)
            {
                using (UndoManager.Self.RequestLock())
                {

                    element.Instances.Remove(whatToInsert);
                    int whereToInsert = element.Instances.IndexOf(whatToMoveInFrontOf) + 1;

                    element.Instances.Insert(whereToInsert, whatToInsert);

                    RefreshInResponseToReorder(whatToMoveInFrontOf);
                    GumCommands.Self.FileCommands.TryAutoSaveElement(element);
                }
            }
        }
        private void RefreshInResponseToReorder(InstanceSave instance)
        {
            var element = _selectedState.SelectedElement;

            GumCommands.Self.GuiCommands.RefreshElementTreeView(element);


            WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

            PluginManager.Self.InstanceReordered(instance);
        }
    }
}
