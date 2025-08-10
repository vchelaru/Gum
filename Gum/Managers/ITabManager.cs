using System.Windows;
using Gum.Plugins;

namespace Gum.Managers;

public interface ITabManager
{
    PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation);
    PluginTab AddControl(FrameworkElement element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
    void RemoveControl(FrameworkElement element);
    bool ShowTabForControl(System.Windows.Controls.UserControl control);
    
    #region These will move to the PluginTab
    void HideTab(PluginTab tab);
    void ShowTab(PluginTab tab, bool focus = true);
    bool IsTabVisible(PluginTab tab);
    bool IsTabFocused(PluginTab tab);
    #endregion
}
