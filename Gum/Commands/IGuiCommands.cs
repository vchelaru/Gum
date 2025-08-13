using Gum.Controls;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Commands;
public interface IGuiCommands
{
    System.Windows.Forms.Cursor AddCursor { get; set; }
    void Initialize(MainPanelControl mainPanelControl);
    void BroadcastRefreshBehaviorView();
    void RefreshStateTreeView();
    void RefreshVariables(bool force = false);
    void RefreshVariableValues();
    int UiZoomValue { get; set; }
    void RefreshElementTreeView();
    void RefreshElementTreeView(IInstanceContainer instanceContainer);
    void MoveToCursor(System.Windows.Window window);
    void PrintOutput(string output);
    void ToggleToolVisibility();
    void FocusSearch();
    Spinner ShowSpinner();
}
