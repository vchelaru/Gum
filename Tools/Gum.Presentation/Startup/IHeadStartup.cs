using Gum.Settings;

namespace Gum.Startup;

/// <summary>
/// The framework-specific steps of tool startup, called by <see cref="GumStartupSequence"/> at
/// fixed points in its order. The WPF head implements these against its views and its MEF
/// plugin host; the Avalonia head implements them against its own.
/// </summary>
public interface IHeadStartup
{
    /// <summary>
    /// Copies settings from the legacy per-user settings file into the head's settings store
    /// the first time the head runs. Called right after the legacy file is loaded.
    /// </summary>
    void MigrateLegacySettings(GeneralSettingsFile legacySettings);

    /// <summary>Initializes the element tree view. Called after the type manager, before the wireframe.</summary>
    void InitializeElementTreeView();

    /// <summary>Initializes the property grid. Called after the wireframe, before plugins.</summary>
    void InitializePropertyGrid();

    /// <summary>Loads and starts plugins. Called before the standard elements are initialized.</summary>
    void InitializePlugins();

    /// <summary>
    /// Tells plugins the render surface exists, so wireframe controls are ready before a project
    /// loads. Called immediately before the project manager initializes.
    /// </summary>
    void RenderSurfaceReady();
}
