using System;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Commands;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Plugins.AlignmentButtons;

public class CommonControlLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;

    public CommonControlLogic(ISelectedState selectedState, IWireframeCommands wireframeCommands,
        IGuiCommands guiCommands, IFileCommands fileCommands, ISetVariableLogic setVariableLogic)
    {
        _selectedState = selectedState;
        _wireframeCommands = wireframeCommands;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
    }

    static bool InheritsFromText(InstanceSave instance) =>
        ObjectFinder.Self.GetRootStandardElementSave(instance)?.Name == "Text";
    public void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits, float value = 0f)
    {
        if(value == 0f)
        {
            // remove the negative
            value = 0f;
        }
        SetAndCallReact("X", value, "float");
        SetAndCallReact("XOrigin", alignment, "HorizontalAlignment");
        SetAndCallReact("XUnits", xUnits, typeof(Gum.Managers.PositionUnitType).Name);

        // Decided per instance: a mixed selection aligns each Text's text and gives the other
        // instances no HorizontalAlignment, which they do not use.
        SetAndCallReact("HorizontalAlignment", alignment, "HorizontalAlignment", InheritsFromText);

    }

    public void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits, float value = 0f)
    {
        if (value == 0f)
        {
            // remove the negative
            value = 0f;
        }
        SetAndCallReact("Y", value, "float");
        SetAndCallReact("YOrigin", alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
        SetAndCallReact("YUnits", yUnits, typeof(PositionUnitType).Name);

        // Decided per instance, as for HorizontalAlignment above.
        SetAndCallReact("VerticalAlignment", alignment, "VerticalAlignment", InheritsFromText);

    }

    public void SetAndCallReact(string unqualified, object value, string typeName) =>
        SetAndCallReact(unqualified, value, typeName, instanceFilter: null);

    /// <summary>
    /// Writes the variable on each selected instance that is unlocked and passes
    /// <paramref name="instanceFilter"/>. With no instance selected it writes the element's own
    /// variable, but only without a filter, since a filter marks an instance-only variable.
    /// </summary>
    private void SetAndCallReact(string unqualified, object value, string typeName, Func<InstanceSave, bool>? instanceFilter)
    {
        // The Alignment tab is only shown while a state is selected.
        if (_selectedState.SelectedStateSave is not { } state)
        {
            return;
        }

        bool handledByInstance = false;
        foreach(var instance in _selectedState.SelectedInstances)
        {
            handledByInstance = true;
            // A locked instance keeps its position and size. It still counts as handled so the
            // write does not fall through to the element.
            if (instance.Locked || instanceFilter?.Invoke(instance) == false)
            {
                continue;
            }
            string GetVariablePrefix()
            {
                string prefixInternal = "";
                if (instance != null)
                {
                    prefixInternal = instance.Name + ".";
                }
                return prefixInternal;
            }
            string prefix = GetVariablePrefix();

            var oldValue = state.GetValue(prefix + unqualified);
            state.SetValue(prefix + unqualified, value, typeName);

            // do this so the SetVariableLogic doesn't attempt to hold the object in-place which causes all kinds of weirdness
            RecordSetVariablePersistPositions();
            _setVariableLogic.ReactToPropertyValueChanged(unqualified, oldValue, _selectedState.SelectedElement, instance, state, refresh: false);
            ResumeSetVariablePersistOptions();
        }

        if(!handledByInstance && instanceFilter == null)
        {
            if (_selectedState.SelectedComponent != null || _selectedState.SelectedStandardElement != null)
            {
                var oldValue = state.GetValue(unqualified);
                state.SetValue(unqualified, value, typeName);

                // do this so the SetVariableLogic doesn't attempt to hold the object in-place which causes all kinds of weirdness
                RecordSetVariablePersistPositions();
                _setVariableLogic.ReactToPropertyValueChanged(unqualified, oldValue, _selectedState.SelectedElement, null, state, refresh: false);
                ResumeSetVariablePersistOptions();
            }
        }
    }

    public void RefreshAndSave()
    {
        _guiCommands.RefreshVariables(force: true);
        _wireframeCommands.Refresh();
        _fileCommands.TryAutoSaveCurrentElement();
    }

    bool StoredAttemptToPersistPositionsOnUnitChanges;
    private void RecordSetVariablePersistPositions()
    {
        StoredAttemptToPersistPositionsOnUnitChanges = _setVariableLogic.AttemptToPersistPositionsOnUnitChanges;
        _setVariableLogic.AttemptToPersistPositionsOnUnitChanges = false;
    }

    private void ResumeSetVariablePersistOptions()
    {
        _setVariableLogic.AttemptToPersistPositionsOnUnitChanges = StoredAttemptToPersistPositionsOnUnitChanges;
    }
}
