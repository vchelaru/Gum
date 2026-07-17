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
    bool LoadProject();
    void LoadProject(FilePath fileName);
    bool SaveProject(bool forceSaveContainedElements = false);
    string MakeAbsoluteIfNecessary(string textureAsString);
    bool AskUserForProjectNameIfNecessary(out bool isProjectNew);

    /// <summary>
    /// Shows a dialog informing the user that the given file is read-only and
    /// offers to open the folder containing it.
    /// </summary>
    void ShowReadOnlyDialog(string fileName);
}
