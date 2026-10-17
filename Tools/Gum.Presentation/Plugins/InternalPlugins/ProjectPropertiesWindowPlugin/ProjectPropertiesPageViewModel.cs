namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>The pages the project's settings are split across in the Settings window.</summary>
public enum ProjectPropertiesSection
{
    /// <summary>Texture filter, unit and Android file name restrictions, the single pixel texture.</summary>
    General,

    /// <summary>The default canvas size and the guides drawn on it.</summary>
    Canvas,

    /// <summary>The localization files and whether localization shows in Gum.</summary>
    Localization,
}

/// <summary>
/// The content of one Project page of the Settings window: the shared
/// <see cref="ProjectPropertiesViewModel"/> and which of its settings the page shows.
/// </summary>
public sealed class ProjectPropertiesPageViewModel
{
    /// <summary>Creates a page over <paramref name="properties"/>.</summary>
    public ProjectPropertiesPageViewModel(ProjectPropertiesViewModel properties, ProjectPropertiesSection section)
    {
        Properties = properties;
        Section = section;
    }

    /// <summary>The project settings shared by every Project properties page.</summary>
    public ProjectPropertiesViewModel Properties { get; }

    /// <summary>The settings this page shows.</summary>
    public ProjectPropertiesSection Section { get; }
}
