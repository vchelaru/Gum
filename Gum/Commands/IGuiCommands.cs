using Gum.Controls;
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
    Spinner ShowSpinner();
}
