using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.ToolCommands;
using Gum.Wireframe;
using ToolsUtilities;

namespace Gum.Undo
{
    public class UndoManager
    {
        #region Fields

        Dictionary<ElementSave, Stack<ElementSave>> mUndos = new Dictionary<ElementSave, Stack<ElementSave>>();

        static UndoManager mSelf;

        ElementSave mRecordedElementSave;

        public event EventHandler UndosChanged;

        //StateSave mRecordedStateSave;
        //List<InstanceSave> mRecordedInstanceList;

        #endregion

        public IEnumerable<ElementSave> CurrentUndoStack
        {
            get
            {
                Stack<ElementSave> stack = null;

                if (SelectedState.Self.SelectedElement != null && mUndos.ContainsKey(SelectedState.Self.SelectedElement))
                {
                    stack = mUndos[SelectedState.Self.SelectedElement];
                }

                return stack;
            }
        }

        public static UndoManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new UndoManager();
                }
                return mSelf;
            }
        }

        public void RecordState()
        {

            mRecordedElementSave = null;
            

            if (SelectedState.Self.SelectedElement != null)
            {
                if (SelectedState.Self.SelectedComponent != null)
                {
                    mRecordedElementSave = FileManager.CloneSaveObject(SelectedState.Self.SelectedComponent);
                }
                else if (SelectedState.Self.SelectedScreen != null)
                {
                    mRecordedElementSave = FileManager.CloneSaveObject(SelectedState.Self.SelectedScreen);
                }
                else if (SelectedState.Self.SelectedStandardElement != null)
                {
                    mRecordedElementSave = FileManager.CloneSaveObject(SelectedState.Self.SelectedStandardElement);
                }
            }

            PrintStatus("RecordState");
        }

        public void RecordUndo()
        {
            if (mRecordedElementSave != null && SelectedState.Self.SelectedElement != null)
            {
                StateSave currentStateSave = SelectedState.Self.SelectedStateSave;
                ElementSave selectedElement = SelectedState.Self.SelectedElement;


                bool doStatesDiffer = FileManager.AreSaveObjectsEqual(mRecordedElementSave.DefaultState, currentStateSave) == false;
                bool doStateCategoriesDiffer =
                    FileManager.AreSaveObjectsEqual(mRecordedElementSave.Categories, selectedElement.Categories) == false;
                bool doInstanceListsDiffer = FileManager.AreSaveObjectsEqual(mRecordedElementSave.Instances, selectedElement.Instances) == false;
                bool doTypesDiffer = mRecordedElementSave.BaseType != selectedElement.BaseType;
                bool doNamesDiffer = mRecordedElementSave.Name != selectedElement.Name;

                if (doStatesDiffer || doInstanceListsDiffer || doTypesDiffer || doNamesDiffer)
                {
                    if (mUndos.ContainsKey(SelectedState.Self.SelectedElement) == false)
                    {
                        mUndos.Add(SelectedState.Self.SelectedElement, new Stack<ElementSave>());
                    }

                    if (!doInstanceListsDiffer)
                    {
                        mRecordedElementSave.Instances = null;
                    }
                    if (!doStatesDiffer)
                    {
                        mRecordedElementSave.States = null;
                    }
                    if (!doStateCategoriesDiffer)
                    {
                        mRecordedElementSave.Categories = null;
                    }
                    if (!doNamesDiffer)
                    {
                        mRecordedElementSave.Name = null;
                    }
                    if (!doTypesDiffer)
                    {
                        mRecordedElementSave.BaseType = null;
                    }

                    Stack<ElementSave> stack = mUndos[SelectedState.Self.SelectedElement] ;

                    stack.Push(mRecordedElementSave);
                    RecordState();

                    UndosChanged?.Invoke(this, null);
                }


                PrintStatus("RecordUndo");
            }

        }

        public void PerformUndo()
        {
            Stack<ElementSave> stack = null;

            if (SelectedState.Self.SelectedElement != null && mUndos.ContainsKey(SelectedState.Self.SelectedElement))
            {
                stack = mUndos[SelectedState.Self.SelectedElement];
            }

            if (stack != null && stack.Count != 0)
            {
                ElementSave lastSelectedElementSave = SelectedState.Self.SelectedElement;

                ElementSave undoObject = stack.Pop();

                ElementSave toApplyTo = SelectedState.Self.SelectedElement;

                bool shouldRefreshWireframe = false;

                if (undoObject.DefaultState != null)
                {
                    Apply(undoObject.DefaultState, toApplyTo.DefaultState, toApplyTo);
                }
                if (undoObject.Instances != null)
                {
                    Apply(undoObject.Instances, toApplyTo.Instances, toApplyTo);
                    shouldRefreshWireframe = true;
                }
                if (undoObject.Name != null)
                {
                    string oldName = toApplyTo.Name;
                    toApplyTo.Name = undoObject.Name;
                    RenameManager.Self.HandleRename(toApplyTo, (InstanceSave)null, oldName);
                }
                if (undoObject.BaseType != null)
                {
                    string oldBaseType = toApplyTo.BaseType;
                    toApplyTo.BaseType = undoObject.BaseType;
                    
                    toApplyTo.ReactToChangedBaseType(null, oldBaseType);
                }

                RecordState();


                UndosChanged?.Invoke(this, null);

                GumCommands.Self.GuiCommands.RefreshElementTreeView();
                SelectedState.Self.UpdateToSelectedStateSave();

                // The instances may have changed.  We will want 
                // to refresh the wireframe since the IPSOs in the 
                // wireframe have tags.
                if (shouldRefreshWireframe)
                {
                    WireframeObjectManager.Self.RefreshAll(true);
                }

                PrintStatus("PerformUndo");

                // If an instance is removed
                // through an undo and if that
                // instance is the selected instance
                // then we want to refresh that.
                if (lastSelectedElementSave != null && SelectedState.Self.SelectedElement == null)
                {
                    SelectedState.Self.SelectedElement = lastSelectedElementSave;
                }

                GumCommands.Self.FileCommands.TryAutoSaveProject();
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

                ElementTreeViewManager.Self.VerifyComponentsAreInTreeView(ProjectManager.Self.GumProjectSave);
            }
        }

        // Normally we wouldn't do this in the pattern but the originator itself
        // can't be found without project-wide knowledge...so we have to do some type-specific
        // logic.
        ElementSave GetWhatToApplyTo(UndoObject undoObject)
        {
            object parentAsObject = undoObject.Parent;
            ElementSave parentAsElementSave = parentAsObject as ElementSave;
            return parentAsElementSave;

            //if (undoObject.StateSave != null)
            //{
            //    if (parentAsElementSave != null)
            //    {
            //        // for now we will just assume the default state
            //        return parentAsElementSave.DefaultState;
            //    }
            //}
            //if (undoObject.Instances != null)
            //{
            //    if (parentAsElementSave != null)
            //    {
            //        return parentAsElementSave.Instances;
            //    }
            //}

            //return null;

        }

        void Apply(object undoObject, object toApplyTo, ElementSave parent)
        {
            if (toApplyTo != null && toApplyTo is StateSave && undoObject is StateSave)
            {
                StateSave undoStateSave = undoObject as StateSave;
                StateSave toApplyToStateSave = toApplyTo as StateSave;


                toApplyToStateSave.SetFrom(undoStateSave);
            }
            else if (toApplyTo != null && toApplyTo is List<InstanceSave> && undoObject is List<InstanceSave>)
            {
                List<InstanceSave> listToApplyTo = (List<InstanceSave>)toApplyTo;
                List<InstanceSave> undoList = (List<InstanceSave>)undoObject;

                listToApplyTo.Clear();

                foreach (var instance in undoList)
                {
                    instance.ParentContainer = parent;
                    listToApplyTo.Add(instance);
                }
            }
        }

        void PrintStatus(string reason)
        {
            Stack<ElementSave> stack = null;

            if (SelectedState.Self.SelectedElement != null)
            {
                if (mUndos.ContainsKey(SelectedState.Self.SelectedElement))
                {
                    stack = mUndos[SelectedState.Self.SelectedElement];
                }

                if (stack == null)
                {
                    System.Console.Out.WriteLine("No undos for " + SelectedState.Self.SelectedElement);

                }
                else
                {
                    string whatToWrite = reason + "\n\tUndos: " + stack.Count;

                    System.Console.Out.WriteLine(whatToWrite);
                }
            }
        }

        public void ClearAll()
        {
            mUndos.Clear();
        }
    }
}
