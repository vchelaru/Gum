namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Supplies the current project directory on demand. Services that need the
/// directory should take this abstraction instead of capturing a string at
/// construction — the tool swaps directories when the user opens a different
/// project, and singleton services must reflect that change.
/// </summary>
public interface IProjectDirectoryProvider
{
    /// <summary>
    /// The current project directory, or null when no project is loaded.
    /// </summary>
    string? ProjectDirectory { get; }
}

/// <summary>
/// Simple fixed-value <see cref="IProjectDirectoryProvider"/> for contexts
/// that never switch projects (e.g. the CLI).
/// </summary>
public class FixedProjectDirectoryProvider : IProjectDirectoryProvider
{
    /// <inheritdoc/>
    public string? ProjectDirectory { get; }

    public FixedProjectDirectoryProvider(string? projectDirectory)
    {
        ProjectDirectory = projectDirectory;
    }
}
