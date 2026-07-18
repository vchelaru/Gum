using Gum.DataTypes;
using Gum.DataTypes.Variables;
using ToolsUtilities;

namespace Gum.Services;

public interface IExposeVariableService
{
    /// <summary>
    /// Exposes a single instance variable, prompting the user for the exposed name via a dialog. Used by the
    /// standalone "Expose Variable" row context menu.
    /// </summary>
    OptionallyAttemptedGeneralResponse<VariableSave> HandleExposeVariableClick(InstanceSave instanceSave, string rootVariableName);

    /// <summary>
    /// Exposes a single instance variable under a caller-supplied name, without prompting. Used by callers that
    /// already determined the exposed name (e.g. the composite color swatch's single-prompt expose, which derives
    /// every channel's name from one shared base name).
    /// </summary>
    OptionallyAttemptedGeneralResponse<VariableSave> ExposeVariable(InstanceSave instanceSave, string rootVariableName, string exposedName);

    void HandleUnexposeVariableClick(VariableSave variableSave, ElementSave elementSave);
}
