using System.Collections.Generic;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Handles user-facing decisions and feedback during code generation.
/// In the Gum tool, this shows dialogs. In CLI mode, this auto-confirms and logs.
/// </summary>
public interface ICodeGenResultHandler
{
    /// <summary>
    /// Shows an informational message to the user.
    /// </summary>
    void ShowMessage(string message);

    /// <summary>
    /// Asks the user a yes/no question. Returns true if yes.
    /// </summary>
    bool ShowYesNoMessage(string message, string caption);

    /// <summary>
    /// Logs output (non-dialog).
    /// </summary>
    void PrintOutput(string message);

    /// <summary>
    /// Called when elements referenced by the selected element are missing generated code files.
    /// Returns true if the missing files should be auto-generated.
    /// </summary>
    bool ShouldGenerateMissingFiles(IReadOnlyList<string> missingElementNames, bool showPopups);
}
