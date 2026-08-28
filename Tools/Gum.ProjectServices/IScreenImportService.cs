using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Headless core of adding a deserialized Screen into a loaded Gum project: conflict-checks by
/// name, adds the <see cref="ElementReference"/> and <see cref="ScreenSave"/>, sorts both lists,
/// and initializes the screen. Shared by the tool's Content → Import → Screen path (which layers
/// a conflict dialog, selection, and autosave on top — see
/// <c>Gum.Plugins.ImportPlugin.Manager.ImportLogic</c>) and <c>gumcli import-screen</c> (headless).
/// </summary>
public interface IScreenImportService
{
    /// <summary>
    /// Adds <paramref name="screenSave"/> to <paramref name="project"/>. Returns a failed result
    /// with <see cref="ScreenImportResult.ConflictingScreenName"/> set if a screen or component
    /// already exists under that name — checked via <c>ObjectFinder.Self</c>, so callers must
    /// point it at <paramref name="project"/> before calling.
    /// </summary>
    ScreenImportResult ImportScreen(GumProjectSave project, ScreenSave screenSave);
}
