using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using Gum.ToolStates;
using Gum.Services;

namespace Gum.Undo;

[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class UndoPlugin : InternalPlugin
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    public UndoPlugin() 
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _undoManager = Locator.GetRequiredService<IUndoManager>();
    }

    public override void StartUp()
    {
        this.ElementSelected += HandleElementSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ProjectLoad += HandleProjectLoad;
        this.InstanceAdd += HandleInstanceAdd;
        this.InstanceDelete += HandleInstanceDelete;
        this.InstancesDelete += HandleInstancesDelete;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;

        this.BehaviorSelected += HandleBehaviorSelected;
        this.BehaviorInstanceAdd += HandleBehaviorInstanceAddForUndo;
        this.StateAdd += HandleStateAddForUndo;
        this.StateDelete += HandleStateDeleteForUndo;
        this.CategoryAdd += HandleCategoryAddForUndo;
        this.CategoryDelete += HandleCategoryDeleteForUndo;
        this.InstanceRename += HandleInstanceRenameForBehaviorUndo;
    }

    ElementSave? lastSelectedElement;

    private void HandleStateSelected(StateSave save)
    {
        _undoManager.RecordState();
        OptionallyBroadcastUndosChanged();
    }

    private void HandleBehaviorReferencesChanged(ElementSave obj)
    {
        _undoManager.RecordUndo();
        OptionallyBroadcastUndosChanged();
    }

    void HandleInstancesDelete(ElementSave arg1, InstanceSave[] arg2)
    {
        _undoManager.RecordUndo();
        OptionallyBroadcastUndosChanged();
    }

    void HandleInstanceDelete(ElementSave arg1, InstanceSave arg2)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
        else
        {
            _undoManager.RecordUndo();
            OptionallyBroadcastUndosChanged();
        }
    }

    void HandleInstanceAdd(ElementSave arg1, InstanceSave arg2)
    {
        _undoManager.RecordUndo();
        OptionallyBroadcastUndosChanged();
    }

    void HandleProjectLoad(DataTypes.GumProjectSave obj)
    {
        _undoManager.ClearAll();
        OptionallyBroadcastUndosChanged();
    }

    void HandleElementSelected(DataTypes.ElementSave obj)
    {
        _undoManager.RecordState();
        OptionallyBroadcastUndosChanged();
    }

    void HandleInstanceSelected(DataTypes.ElementSave elementSave, InstanceSave instanceSave)
    {
        _undoManager.RecordState();

        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorState();
        }

        // the instance could have changed the element, so broadcast anyway
        OptionallyBroadcastUndosChanged();
    }

    private void HandleBehaviorSelected(BehaviorSave? behavior)
    {
        _undoManager.RecordBehaviorState();
        _undoManager.BroadcastUndosChanged();
    }

    private void HandleStateAddForUndo(StateSave state)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
    }

    private void HandleStateDeleteForUndo(StateSave state)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
    }

    private void HandleCategoryAddForUndo(StateSaveCategory category)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
    }

    private void HandleCategoryDeleteForUndo(StateSaveCategory category)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
    }

    private void HandleBehaviorInstanceAddForUndo(BehaviorSave behavior, BehaviorInstanceSave instance)
    {
        _undoManager.RecordBehaviorUndo();
    }

    private void HandleInstanceRenameForBehaviorUndo(ElementSave element, InstanceSave instance, string oldName)
    {
        if (_selectedState.SelectedBehavior != null)
        {
            _undoManager.RecordBehaviorUndo();
        }
    }

    private void OptionallyBroadcastUndosChanged()
    {
        if (_selectedState.SelectedElement != lastSelectedElement)
        {
            _undoManager.BroadcastUndosChanged();
            lastSelectedElement = _selectedState.SelectedElement;
        }
    }
}
