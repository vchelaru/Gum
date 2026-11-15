using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// The outcome of loading an engine profile. <see cref="Profile"/> is never null: when the file could not be
/// used it is unrestricted and <see cref="Warning"/> says why.
/// </summary>
public class EngineProfileLoadResult
{
    public EngineProfileLoadResult(EngineProfile profile, FilePath? filePath, string? warning)
    {
        Profile = profile;
        FilePath = filePath;
        Warning = warning;
    }

    /// <summary>The profile to apply; unrestricted when the reference is empty or the file is unusable.</summary>
    public EngineProfile Profile { get; }

    /// <summary>The resolved profile file, or null when the project has no reference or no location to resolve it against.</summary>
    public FilePath? FilePath { get; }

    /// <summary>A one-sentence problem for the Output tab, or null when the profile loaded or there is none.</summary>
    public string? Warning { get; }
}

/// <summary>
/// Reads the <c>.gumengine</c> file a project references. Never throws.
/// </summary>
public interface IEngineProfileLoader
{
    /// <summary>
    /// Loads the profile named by <paramref name="profileReference"/>, a path relative to the folder of
    /// <paramref name="projectFileName"/>. An empty reference, or a project with no file yet, is unrestricted
    /// without a warning; a missing, unreadable or invalid file is unrestricted with one.
    /// </summary>
    EngineProfileLoadResult Load(string? projectFileName, string profileReference);
}
