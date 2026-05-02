namespace Gum.Bundle;

/// <summary>
/// Non-fatal warning emitted by <see cref="GumProjectDependencyWalker"/> when the project
/// references a file that is not present on disk. The walker does not throw — callers
/// (e.g. `gumcli pack`) decide whether to treat missing files as fatal.
/// </summary>
public class DependencyWarning
{
    /// <summary>The bundle-relative path the project asked for (forward slashes).</summary>
    public string ReferencedPath { get; }

    /// <summary>
    /// Best-effort name of the element (or `Element.Instance`) that referenced the missing file.
    /// May be empty when the source could not be attributed to a specific element.
    /// </summary>
    public string ReferencedFromElementName { get; }

    /// <summary>Human-readable message describing the warning.</summary>
    public string Message { get; }

    /// <summary>Initializes a new <see cref="DependencyWarning"/>.</summary>
    public DependencyWarning(string referencedPath, string referencedFromElementName, string message)
    {
        ReferencedPath = referencedPath;
        ReferencedFromElementName = referencedFromElementName;
        Message = message;
    }
}
