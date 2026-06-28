using Gum.DataTypes.Variables;
using ToolsUtilities;

namespace Gum.Undo;

internal static class UndoStateHelper
{
    // Copies a state's name, variables, and variable lists onto another state. This was the tool-only
    // StateSave.SetFrom extension; it moved into the headless layer along with UndoManager (ADR-0005
    // Phase 3) since UndoManager was its only caller. FixEnumerations is called unconditionally,
    // matching the tool's prior behavior (the extension guarded it with #if GUM, and GUM is defined in
    // the tool build). Shared by ElementUndoStrategy (applying state variables) and BehaviorUndoStrategy
    // (applying a behavior's RequiredVariables).
    public static void SetStateContentsFrom(StateSave stateSave, StateSave otherStateSave)
    {
        stateSave.Name = otherStateSave.Name;

        stateSave.Variables.Clear();
        stateSave.VariableLists.Clear();

        foreach (VariableSave variable in otherStateSave.Variables)
        {
            stateSave.Variables.Add(variable.Clone());
        }

        foreach (VariableListSave variableList in otherStateSave.VariableLists)
        {
            stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
        }

        stateSave.FixEnumerations();
    }
}
