namespace Gum.Plugins;

/// <summary>
/// Tracks which plugins the user has disabled, persisting the choice to disk across sessions.
/// Plugin identity crosses this seam as a plain <see cref="string"/> unique id, so this interface
/// (and its implementation) has no MEF/WinForms dependency and can live in the headless
/// Gum.Presentation assembly - unlike <see cref="PluginManager"/> itself, which legitimately stays
/// tool-side because it owns the live MEF composition machinery.
/// </summary>
public interface IPluginEnablementStore
{
    /// <summary>
    /// Loads the persisted disabled-plugin list from disk, or starts empty if no file exists yet.
    /// Must be called once (mirrors the tool's two-stage init pattern) before the other members are
    /// meaningful.
    /// </summary>
    void Load();

    /// <summary>Whether the given plugin was previously disabled by the user.</summary>
    bool IsDisabled(string pluginUniqueId);

    /// <summary>Marks the given plugin disabled and persists the change. Idempotent.</summary>
    void Disable(string pluginUniqueId);

    /// <summary>Marks the given plugin enabled and persists the change. Idempotent.</summary>
    void Enable(string pluginUniqueId);

    /// <summary>Re-persists the current state unconditionally.</summary>
    void Save();
}
