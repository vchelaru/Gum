using Gum.DataTypes;

namespace Gum.Commands;
public interface IGuiCommands
{
    void BroadcastRefreshBehaviorView();
    void RefreshStateTreeView();
    void RefreshVariables(bool force = false);
    void RefreshVariableValues();
    void RefreshElementTreeView();
    void RefreshElementTreeView(IInstanceContainer instanceContainer);
    void PrintOutput(string output);
    void ToggleToolVisibility();
    void FocusSearch();

    /// <summary>
    /// Shows a progress spinner and returns it as a framework-neutral <see cref="ISpinner"/>.
    /// </summary>
    ISpinner ShowSpinner();

    /// <summary>
    /// Brings the tool's main window to the foreground. Used to reclaim focus after an external
    /// process (e.g. bmfont.exe) that briefly took its own top-level window has exited.
    /// </summary>
    void ActivateMainWindow();
}
