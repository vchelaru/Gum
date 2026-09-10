using Gum.Settings;

namespace Gum.Managers;

/// <summary>
/// Manages loading and saving per-project user settings (.user.setj files)
/// </summary>
public interface IUserProjectSettingsManager
{
    /// <summary>
    /// Gets the current project settings, or null if no project is loaded
    /// </summary>
    UserProjectSettings? CurrentSettings { get; }

    /// <summary>
    /// Load settings for the given .gumx file path.
    /// Returns new empty settings if file doesn't exist or on error.
    /// </summary>
    void LoadForProject(string gumxFilePath);

    /// <summary>
    /// Save current settings to disk.
    /// </summary>
    void Save();

    /// <summary>
    /// Clear current settings (on project close).
    /// </summary>
    void Clear();
}
