using StateAnimationPlugin.Models;

namespace StateAnimationPlugin.Managers
{
    /// <summary>
    /// Loads and persists the State Animation plugin's global settings.
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// The current global settings. Populated by <see cref="LoadOrCreateSettings"/> and
        /// persisted by <see cref="SaveSettings"/>.
        /// </summary>
        AnimationPluginSettings GlobalSettings { get; }

        /// <summary>
        /// Loads the global settings from disk, falling back to defaults when no settings file exists.
        /// </summary>
        void LoadOrCreateSettings();

        /// <summary>
        /// Writes the current global settings to disk, creating the containing directory if needed.
        /// </summary>
        void SaveSettings();
    }
}
