namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Abstraction for logging output and errors during code generation.
/// In the Gum tool, this delegates to IOutputManager. In CLI mode, this writes to console.
/// </summary>
public interface ICodeGenLogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void PrintOutput(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void PrintError(string message);
}
