using System.IO;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Plugins;

/// <inheritdoc cref="IPluginEnablementStore"/>
public class PluginEnablementStore : IPluginEnablementStore
{
    private readonly string _fileName;
    private PluginSettingsSave _settings = new();

    /// <summary>Persists to the default per-user Gum plugin settings file.</summary>
    public PluginEnablementStore() : this(DefaultFileName)
    {
    }

    /// <summary>Persists to the given file - mainly for tests that need an isolated file.</summary>
    public PluginEnablementStore(string fileName)
    {
        _fileName = fileName;
    }

    private static string DefaultFileName =>
        FileManager.UserApplicationDataForThisApplication + "GumPluginSettings.xml";

    public void Load()
    {
        if (!File.Exists(_fileName))
        {
            _settings = new PluginSettingsSave();
            return;
        }

        try
        {
            _settings = PluginSettingsSave.Load(_fileName);
        }
        catch (IOException)
        {
            // Loaded during startup, so an unreadable file enables every plugin rather than stopping
            // the tool. The next save overwrites it, so keep a copy.
            File.Copy(_fileName, _fileName + ".unreadable", overwrite: true);
            _settings = new PluginSettingsSave();
        }
    }

    public bool IsDisabled(string pluginUniqueId) =>
        _settings.DisabledPlugins.Contains(pluginUniqueId);

    public void Disable(string pluginUniqueId)
    {
        if (!_settings.DisabledPlugins.Contains(pluginUniqueId))
        {
            _settings.DisabledPlugins.Add(pluginUniqueId);
            Save();
        }
    }

    public void Enable(string pluginUniqueId)
    {
        if (_settings.DisabledPlugins.Remove(pluginUniqueId))
        {
            Save();
        }
    }

    public void Save() => _settings.Save(_fileName);
}
