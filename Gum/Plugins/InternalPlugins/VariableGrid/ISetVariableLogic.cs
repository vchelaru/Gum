using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public interface ISetVariableLogic
{
    bool AttemptToPersistPositionsOnUnitChanges { get; set; }

    GeneralResponse PropertyValueChanged(string unqualifiedMemberName, object? oldValue,
        InstanceSave instance, StateSave stateContainingVariable, bool refresh = true, bool recordUndo = true,
        bool trySave = true, bool isFullCommit = true);

    GeneralResponse ReactToPropertyValueChanged(string unqualifiedMember, object? oldValue, IInstanceContainer instanceContainer,
        InstanceSave instance, StateSave currentState, bool refresh, bool recordUndo = true, bool trySave = true, bool isFullCommit = true);

    GeneralResponse PropertyValueChangedOnBehaviorInstance(string memberName, object? oldValue,
        BehaviorSave behavior, BehaviorInstanceSave instance);

    /// <summary>
    /// Sets a pre-made copy decision for the current batch of file drops. When non-null,
    /// the "copy or reference?" dialog is suppressed and this value is used instead.
    /// Pass null to clear the batch decision and restore per-file dialog prompts.
    /// </summary>
    void SetBatchFileCopyDecision(bool? shouldCopy);
}
