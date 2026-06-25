using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

// These rename-change data types were relocated out of the tool assembly's Gum/Logic/RenameLogic.cs
// into the headless Gum.Presentation assembly (ADR-0005 Phase 3) so the undo subsystem's narrow
// IUndoRenameLogic port can reference them without depending on the tool. They are framework-neutral
// (GumDataTypes/ToolsUtilities only). The namespace is intentionally kept as Gum.Logic so the many
// tool-side consumers compile unchanged.

#region Enums

public enum NameChangeAction
{
    Move,
    Rename
}

public enum SideOfEquals
{
    Left,
    Right,
    Both
}

#endregion

#region VariableChange Class

public class VariableChange
{
    public IStateContainer Container;
    public StateSaveCategory Category;
    public StateSave State;
    public VariableSave Variable;
    public object NewValue;

}

public class VariableReferenceChange
{
    public ElementSave Container;
    public VariableListSave VariableReferenceList;
    public int LineIndex;
    public SideOfEquals ChangedSide;
}

public class VariableChangeResponse
{
    public List<VariableChange> VariableChanges = new List<VariableChange>();
    public List<VariableReferenceChange> VariableReferenceChanges = new List<VariableReferenceChange>();

    public string GetChangesDetails()
    {
        var details = string.Empty;

        if (VariableChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "This will also rename the following variables:";
            foreach (var change in VariableChanges)
            {
                var containerName = change.Container is ElementSave elementSave
                    ? elementSave.Name
                    : change.Container.ToString();
                details += $"\n• {change.Variable.Name} in {containerName}";
            }
        }

        if (VariableReferenceChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(details)) details += "\n\n";
            details += "This will also modify the following variable references:";
            foreach (var change in VariableReferenceChanges)
            {
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    details += $"\n• {line} in {change.Container.Name}";
                }
                catch { }
            }
        }

        return details;
    }
}

#endregion
