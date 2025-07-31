using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using System.Management.Instrumentation;
using Gum.Commands;
using Gum.Services;

namespace Gum.Plugins.AlignmentButtons;

public class CommonControlLogic
{
    private readonly ISelectedState _selectedState;
    private readonly WireframeCommands _wireframeCommands;
    private readonly GuiCommands _guiCommands;
    private readonly FileCommands _fileCommands;
    
    public CommonControlLogic()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
        _guiCommands = Locator.GetRequiredService<GuiCommands>();
        _fileCommands = Locator.GetRequiredService<FileCommands>();
    }

    bool SelectionInheritsFromText()
    {
        if(_selectedState.SelectedInstance != null)
        {
            return ObjectFinder.Self.GetRootStandardElementSave(_selectedState.SelectedInstance)?.Name == "Text";
        }
        return false;
    }
    public void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits, float value = 0f)
    {
        SetAndCallReact("X", value, "float");
        SetAndCallReact("XOrigin", alignment, "HorizontalAlignment");
        SetAndCallReact("XUnits", xUnits, typeof(Gum.Managers.PositionUnitType).Name);

        if (SelectionInheritsFromText())
        {
            SetAndCallReact("HorizontalAlignment", alignment, "HorizontalAlignment");
        }

    }

    public void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits, float value = 0f)
    {
        var state = _selectedState.SelectedStateSave;

        SetAndCallReact("Y", value, "float");
        SetAndCallReact("YOrigin", alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
        SetAndCallReact("YUnits", yUnits, typeof(PositionUnitType).Name);

        if (SelectionInheritsFromText())
        {
            SetAndCallReact("VerticalAlignment", alignment, "VerticalAlignment");
        }

    }

    public void SetAndCallReact(string unqualified, object value, string typeName)
    {
        bool handledByInstance = false;
        foreach(var instance in _selectedState.SelectedInstances)
        {
            handledByInstance = true;
            string GetVariablePrefix()
            {
                string prefixInternal = "";
                if (instance != null)
                {
                    prefixInternal = instance.Name + ".";
                }
                return prefixInternal;
            }
            var state = _selectedState.SelectedStateSave;
            string prefix = GetVariablePrefix();

            var oldValue = state.GetValue(prefix + unqualified);
            state.SetValue(prefix + unqualified, value, typeName);

            // do this so the SetVariableLogic doesn't attempt to hold the object in-place which causes all kinds of weirdness
            RecordSetVariablePersistPositions();
            SetVariableLogic.Self.ReactToPropertyValueChanged(unqualified, oldValue, _selectedState.SelectedElement, instance, _selectedState.SelectedStateSave, refresh: false);
            ResumeSetVariablePersistOptions();
        }

        if(!handledByInstance)
        {
            if (_selectedState.SelectedComponent != null || _selectedState.SelectedStandardElement != null)
            {
                var state = _selectedState.SelectedStateSave;

                var oldValue = state.GetValue(unqualified);
                state.SetValue(unqualified, value, typeName);

                // do this so the SetVariableLogic doesn't attempt to hold the object in-place which causes all kinds of weirdness
                RecordSetVariablePersistPositions();
                SetVariableLogic.Self.ReactToPropertyValueChanged(unqualified, oldValue, _selectedState.SelectedElement, null, _selectedState.SelectedStateSave, refresh: false);
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
        StoredAttemptToPersistPositionsOnUnitChanges = SetVariableLogic.Self.AttemptToPersistPositionsOnUnitChanges;
        SetVariableLogic.Self.AttemptToPersistPositionsOnUnitChanges = false;
    }

    private void ResumeSetVariablePersistOptions()
    {
        SetVariableLogic.Self.AttemptToPersistPositionsOnUnitChanges = StoredAttemptToPersistPositionsOnUnitChanges;
    }
}
