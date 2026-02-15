using Gum.Services;
using Gum.Settings;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Gum.Managers;

/// <summary>
/// Manages loading and saving per-project user settings (.user.setj files)
/// </summary>
public class UserProjectSettingsManager : IUserProjectSettingsManager
{
    private readonly IOutputManager _outputManager;
    private UserProjectSettings? _currentSettings;
    private string? _currentFilePath;

    public UserProjectSettings? CurrentSettings => _currentSettings;

    public UserProjectSettingsManager(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    /// <summary>
    /// Load settings for the given .gumx file path.
    /// Returns new empty settings if file doesn't exist or on error.
    /// </summary>
    public void LoadForProject(string gumxFilePath)
    {
        _currentFilePath = GetSettingsFilePath(gumxFilePath);

        if (!File.Exists(_currentFilePath))
        {
            _currentSettings = new UserProjectSettings();
            return;
        }

        try
        {
            string json = File.ReadAllText(_currentFilePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _currentSettings = JsonSerializer.Deserialize<UserProjectSettings>(json);

            // If deserialization returns null, create new settings
            if (_currentSettings == null)
            {
                _currentSettings = new UserProjectSettings();
                _outputManager.AddOutput($"Failed to deserialize user settings from {_currentFilePath}. Using defaults.");
            }
        }
        catch (Exception ex)
        {
            _outputManager.AddError($"Error loading user settings from {_currentFilePath}: {ex.Message}. Using defaults.");
            _currentSettings = new UserProjectSettings();
        }
    }

    /// <summary>
    /// Save current settings to disk.
    /// </summary>
    public void Save()
    {
        if (_currentSettings == null || _currentFilePath == null)
        {
            return;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(_currentSettings, options);
            File.WriteAllText(_currentFilePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex)
        {
            _outputManager.AddError($"Error saving user settings to {_currentFilePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear current settings (on project close).
    /// </summary>
    public void Clear()
    {
        _currentSettings = null;
        _currentFilePath = null;
    }

    private string GetSettingsFilePath(string gumxFilePath)
    {
        // Replace .gumx extension with .user.setj
        return Path.ChangeExtension(gumxFilePath, ".user.setj");
    }
}
