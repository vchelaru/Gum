using System;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli;

/// <summary>
/// Logs code generation output to the console.
/// </summary>
internal class ConsoleCodeGenLogger : ICodeGenLogger
{
    /// <inheritdoc/>
    public void PrintOutput(string message)
    {
        Console.WriteLine(message);
    }

    /// <inheritdoc/>
    public void PrintError(string message)
    {
        Console.Error.WriteLine(message);
    }
}
