namespace Gum.Plugins;

/// <summary>
/// Read-only snapshot of a loaded plugin, used by the "Manage Plugins" dialog
/// (<c>PluginsDialogViewModel</c>). <see cref="PluginHandle"/> is an opaque token — pass it back
/// into <see cref="IPluginManager.DisableUserPlugin"/> / <see cref="IPluginManager.TryEnablePlugin"/>
/// to act on this plugin. It is typed as <see cref="object"/> (the concrete PluginManager casts it
/// back to its own container type) because the real plugin container type lives in the tool-only
/// Gum.csproj, which this headless Gum.Presentation assembly cannot reference.
/// <see cref="CanBeDisabled"/> is false for a plugin the user cannot turn off
/// (<c>PluginBase.CanUserDisable</c>).
/// </summary>
public record PluginSummary(string Name, string DisplayText, bool IsEnabled, bool HasFailureDetails, object PluginHandle,
    bool CanBeDisabled = true);
