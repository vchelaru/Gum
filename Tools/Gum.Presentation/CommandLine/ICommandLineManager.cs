using System.Threading.Tasks;

namespace Gum.CommandLine;

/// <summary>
/// Parses the process command-line arguments and exposes the resulting startup intent
/// (which project/element to load, whether to code-gen or rebuild fonts, and whether the
/// tool should exit immediately rather than show its window). See
/// <see cref="Gum.CommandLine.CommandLineManager"/> for the concrete implementation (tool project).
/// </summary>
public interface ICommandLineManager
{
    /// <summary>
    /// The .gumx project the command line requested be loaded, or null if none was specified.
    /// </summary>
    string GlueProjectToLoad { get; }

    /// <summary>
    /// True when the command line requested an immediate exit after processing (for example a
    /// headless code-gen or font rebuild), so the tool should not run its main window loop.
    /// </summary>
    bool ShouldExitImmediately { get; }

    /// <summary>
    /// True when the command line requested code generation for the whole project.
    /// </summary>
    bool ShouldCodeGenAll { get; }

    /// <summary>
    /// The element the command line requested be selected after load, or null if none was specified.
    /// </summary>
    string ElementName { get; }

    /// <summary>
    /// Parses the process command-line arguments (<see cref="System.Environment.GetCommandLineArgs"/>)
    /// and populates this manager's properties accordingly.
    /// </summary>
    Task ReadCommandLine();
}
