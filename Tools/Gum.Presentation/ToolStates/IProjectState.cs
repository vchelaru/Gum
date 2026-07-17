using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.ToolStates;

public interface IProjectState
{
    GumProjectSave? GumProjectSave { get; }

    /// <summary>
    /// Resolved Standards-palette mode (<c>GeneralSettingsFile.EffectiveUseStandardsPalette</c>).
    /// Narrowed off the whole, WinForms-entangled <c>GeneralSettingsFile</c> (ADR-0005 Phase 3) so
    /// consumers of <see cref="IProjectState"/> can move into the headless Gum.Presentation assembly.
    /// </summary>
    bool EffectiveUseStandardsPalette { get; }

    /// <summary>
    /// The wireframe outline color, read off <c>GeneralSettingsFile.OutlineColorR/G/B</c>. See
    /// <see cref="EffectiveUseStandardsPalette"/> for why this is flattened instead of exposing the
    /// whole settings file.
    /// </summary>
    byte OutlineColorR { get; }
    byte OutlineColorG { get; }
    byte OutlineColorB { get; }

    string? ProjectDirectory { get; }
    FilePath ComponentFilePath { get; }
    FilePath ScreenFilePath { get; }
    FilePath BehaviorFilePath { get; }
    bool NeedsToSaveProject { get; }
}
