using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Settings;
using ToolsUtilities;

namespace Gum.Managers;

public interface IProjectManager
{
    GumProjectSave? GumProjectSave { get; }
    bool HaveErrorsOccurredLoadingProject { get; }

    /// <summary>
    /// True while plugins receive a newly loaded or created project. Anything a plugin changes
    /// then is a load-time fill, not an edit, so auto-saves requested meanwhile are ignored (#5412).
    /// </summary>
    bool IsNotifyingProjectLoad { get; }

    /// <summary>
    /// Whether the project auto-saves on changes. Narrowed off the whole, WinForms-entangled
    /// <c>GeneralSettingsFile</c> (ADR-0005 Phase 3) so <see cref="IProjectManager"/> itself can live
    /// in the headless Gum.Presentation assembly; each member below mirrors the matching one on
    /// <c>GeneralSettingsFile</c>.
    /// </summary>
    bool AutoSave { get; set; }

    /// <summary>
    /// The user's explicit choice for the "Standards palette" UI, or null when they have never
    /// chosen. See <see cref="EffectiveUseStandardsPalette"/> for the resolved value.
    /// </summary>
    bool? UseStandardsPalette { get; set; }

    /// <summary>
    /// The resolved Standards-palette mode. Defaults to on when the user has never made an explicit
    /// choice (<see cref="UseStandardsPalette"/> is null).
    /// </summary>
    bool EffectiveUseStandardsPalette { get; }

    bool ShowTextOutlines { get; }
    int FrameRate { get; }

    byte OutlineColorR { get; }
    byte OutlineColorG { get; }
    byte OutlineColorB { get; }

    byte GuideLineColorR { get; }
    byte GuideLineColorG { get; }
    byte GuideLineColorB { get; }

    byte GuideTextColorR { get; }
    byte GuideTextColorG { get; }
    byte GuideTextColorB { get; }

    /// <summary>
    /// Recently-opened project files. Items are mutated in place (e.g. toggling
    /// <see cref="RecentProjectReference.IsFavorite"/>); call <see cref="SaveGeneralSettings"/> to
    /// persist changes.
    /// </summary>
    IReadOnlyList<RecentProjectReference> RecentProjects { get; }

    /// <summary>
    /// Persists the general settings file, e.g. after mutating <see cref="AutoSave"/>,
    /// <see cref="UseStandardsPalette"/>, or an item in <see cref="RecentProjects"/>.
    /// </summary>
    void SaveGeneralSettings();

    void LoadSettings();
    Task Initialize();
    void CreateNewProject();

    /// <summary>
    /// Builds an empty project with the settings a new project starts with, without loading it or
    /// notifying plugins. Lets the New Project dialog describe what it would create while the
    /// open project stays loaded.
    /// </summary>
    GumProjectSave CreateBlankProject();

    /// <summary>
    /// Asks the user where to save a new project, confirming when the folder is not empty.
    /// </summary>
    /// <returns>The chosen project file, or null if the user cancelled.</returns>
    string? AskUserForNewProjectLocation();

    /// <summary>
    /// Sets the loaded project's file location and tells plugins, as when the user first saves it.
    /// </summary>
    void SetProjectLocation(string fileName);

    Task<bool> LoadProjectAsync();
    Task LoadProjectAsync(FilePath fileName);
    bool SaveProject(bool forceSaveContainedElements = false);
    string MakeAbsoluteIfNecessary(string textureAsString);
    bool AskUserForProjectNameIfNecessary(out bool isProjectNew);

    /// <summary>
    /// Shows a dialog informing the user that the given file is read-only and
    /// offers to open the folder containing it.
    /// </summary>
    void ShowReadOnlyDialog(string fileName);
}
