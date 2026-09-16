using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Launches the GumPreview runtime host against the current project and selected element (issue
/// #4697), and keeps it in sync as the tool's selection changes.
/// </summary>
public interface IPreviewLauncher
{
    /// <summary>
    /// Launches the preview host for the currently selected element, or - if a preview is already
    /// running - pushes the current selection to it instead of starting a second instance. Reports
    /// a friendly error via the output manager when the project isn't saved, nothing is selected, or
    /// the preview executable can't be found.
    /// </summary>
    void Launch();

    /// <summary>
    /// Pushes <paramref name="element"/> to the running preview host so it swaps its root element.
    /// Does nothing if no preview is currently running. Set <paramref name="activate"/> only for an
    /// explicit user action (re-clicking Preview) that should also raise the preview window; leave
    /// it false for passive updates driven by tool selection changes, so picking a different
    /// screen/component in the tool doesn't yank focus away from it (issue #4717).
    /// </summary>
    void PushSelection(ElementSave? element, bool activate = false);

    /// <summary>
    /// Refreshes the running preview's JSON projection after a save (issue #4748). Does nothing
    /// unless a preview is currently running AND it was launched against a temporary JSON copy of a
    /// .gumx project (the Native AOT build can't load .gumx directly) - re-converts the current
    /// in-memory project into that same temp copy so the preview's own hot-reload watcher, which is
    /// watching that directory, picks up the change exactly as it would for a real .gumj project.
    /// </summary>
    void RefreshIfRunning();
}
