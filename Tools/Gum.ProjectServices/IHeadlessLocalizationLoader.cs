using Gum.DataTypes;
using Gum.Localization;

namespace Gum.ProjectServices;

/// <summary>
/// Loads a Gum project's localization files (CSV or RESX) into an
/// <see cref="ILocalizationService"/> in headless contexts such as the CLI,
/// mirroring the load policy used by the Gum tool and the game runtime.
/// </summary>
public interface IHeadlessLocalizationLoader
{
    /// <summary>
    /// Loads the localization files referenced by <paramref name="project"/> into
    /// <paramref name="localizationService"/>, and applies the project's current
    /// language index. Missing or unsupported file combinations are reported through
    /// the logger and skipped without throwing.
    /// </summary>
    /// <param name="project">The loaded Gum project whose LocalizationFiles should be loaded.</param>
    /// <param name="projectDirectory">The directory containing the .gumx file. Localization
    /// file paths in the project are relative to this directory.</param>
    /// <param name="localizationService">The service to populate.</param>
    void LoadLocalizationFiles(GumProjectSave project, string projectDirectory, ILocalizationService localizationService);
}
