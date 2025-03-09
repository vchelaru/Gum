using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;

namespace Gum.Plugins.AlignmentButtons;

public class CommonControlLogic
{
    bool SelectionInheritsFromText()
    {
        if(SelectedState.Self.SelectedInstance != null)
        {
            return ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedInstance)?.Name == "Text";
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
        var state = SelectedState.Self.SelectedStateSave;

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
        foreach(var instance in SelectedState.Self.SelectedInstances)
        {
            string GetVariablePrefix()
            {
                string prefixInternal = "";
                if (instance != null)
                {
                    prefixInternal = instance.Name + ".";
                }
                return prefixInternal;
            }
            var state = SelectedState.Self.SelectedStateSave;
            string prefix = GetVariablePrefix();


            var oldValue = state.GetValue(prefix + unqualified);
            state.SetValue(prefix + unqualified, value, typeName);

            // do this so the SetVariableLogic doesn't attempt to hold the object in-place which causes all kinds of weirdness
            RecordSetVariablePersistPositions();
            SetVariableLogic.Self.ReactToPropertyValueChanged(unqualified, oldValue, SelectedState.Self.SelectedElement, instance, SelectedState.Self.SelectedStateSave, refresh: false);
            ResumeSetVariablePersistOptions();
        }
    }

    public void RefreshAndSave()
    {
        GumCommands.Self.GuiCommands.RefreshVariables(force: true);
        GumCommands.Self.WireframeCommands.Refresh();
        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
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
