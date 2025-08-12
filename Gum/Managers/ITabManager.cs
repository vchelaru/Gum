using System.Windows;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;

namespace Gum.Managers;

public interface ITabManager
{
    PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation);
    PluginTab AddControl(FrameworkElement element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
    void RemoveTab(PluginTab plugin);
}
