using System;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli;

/// <summary>
/// Logs code generation output to the console.
/// </summary>
internal class ConsoleCodeGenLogger : ICodeGenLogger
{
    /// <summary>
    /// How many errors this logger has printed, so a command can fail when the service it was
    /// handed to reported one.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <inheritdoc/>
    public void PrintOutput(string message)
    {
        Console.WriteLine(message);
    }

    /// <inheritdoc/>
    public void PrintError(string message)
    {
        ErrorCount++;
        Console.Error.WriteLine(message);
    }
}
