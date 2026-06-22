using Newtonsoft.Json;
using StateAnimationPlugin.Models;
using System;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers
{
    /// <summary>
    /// Loads and persists the State Animation plugin's global settings to a JSON file.
    /// Instantiated by <see cref="MainStateAnimationPlugin"/>; not an app-wide service.
    /// </summary>
    public class SettingsManager : ISettingsManager
    {
        private readonly FilePath _globalSettingsFilePath;

        /// <inheritdoc/>
        public AnimationPluginSettings GlobalSettings { get; private set; }

        /// <summary>
        /// Creates a new <see cref="SettingsManager"/>.
        /// </summary>
        /// <param name="globalSettingsFilePath">
        /// The file the global settings are read from and written to. When null (the default used
        /// by the running tool), the per-user application-data location is used.
        /// </param>
        public SettingsManager(FilePath? globalSettingsFilePath = null)
        {
            GlobalSettings = new AnimationPluginSettings();
            _globalSettingsFilePath = globalSettingsFilePath ?? new FilePath(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                    @"\Gum\AnimationPlugin\GlobalAnimationSettings.json");
        }

        /// <inheritdoc/>
        public void LoadOrCreateSettings()
        {
            if (_globalSettingsFilePath.Exists())
            {
                var text = System.IO.File.ReadAllText(_globalSettingsFilePath.FullPath);

                GlobalSettings = JsonConvert.DeserializeObject<AnimationPluginSettings>(text) ??
                    new AnimationPluginSettings();
            }

            if (GlobalSettings == null)
            {
                GlobalSettings = new AnimationPluginSettings();
            }
        }

        /// <inheritdoc/>
        public void SaveSettings()
        {
            var text = JsonConvert.SerializeObject(GlobalSettings);

            var directory = _globalSettingsFilePath.GetDirectoryContainingThis();

            if (directory != null)
            {
                System.IO.Directory.CreateDirectory(directory.FullPath);
            }

            System.IO.File.WriteAllText(_globalSettingsFilePath.FullPath, text);
        }
    }
}
