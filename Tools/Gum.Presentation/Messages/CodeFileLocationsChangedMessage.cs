using Gum.ProjectServices.CodeGeneration;

namespace Gum.Messages;

/// <summary>
/// Sent when a Code tab edit changed a setting that moves where code files belong (output library,
/// code project root, generated code folder), so files at the old paths can be migrated (#5846).
/// Never sent for a settings file reloaded from disk.
/// </summary>
public class CodeFileLocationsChangedMessage
{
    /// <summary>The settings before the edit.</summary>
    public CodeOutputProjectSettings Previous { get; }

    /// <summary>The settings after the edit.</summary>
    public CodeOutputProjectSettings Current { get; }

    /// <summary>What changed, e.g. "Output Library from MonoGame (deprecated) to Gum Forms (recommended)".</summary>
    public string Description { get; }

    public CodeFileLocationsChangedMessage(CodeOutputProjectSettings previous, CodeOutputProjectSettings current, string description)
    {
        Previous = previous;
        Current = current;
        Description = description;
    }
}
