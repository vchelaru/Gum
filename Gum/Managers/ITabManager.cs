using Gum.Plugins;
using Gum.Plugins.BaseClasses;

namespace Gum.Managers;

public interface ITabManager
{
    /// <summary>
    /// Adds <paramref name="element"/> as a new tab. Typed as <see cref="object"/> to keep this
    /// interface free of WPF types (issue #3225); the concrete implementation expects a
    /// <c>System.Windows.FrameworkElement</c>.
    /// </summary>
    PluginTab AddControl(object element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
    void RemoveTab(PluginTab plugin);
}
