using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Adapts the Gum tool's IOutputManager to the headless ICodeGenLogger interface.
/// </summary>
internal class ToolCodeGenLogger : ICodeGenLogger
{
    private readonly IOutputManager _outputManager;

    public ToolCodeGenLogger(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    /// <inheritdoc/>
    public void PrintOutput(string message)
    {
        _outputManager.AddOutput(message);
    }

    /// <inheritdoc/>
    public void PrintError(string message)
    {
        _outputManager.AddError(message);
    }
}
