using Gum.Plugins;

namespace Gum.Managers;

public interface ITabManager
{
    /// <summary>
    /// Adds <paramref name="element"/> as a new tab. Typed as <see cref="object"/> to keep this
    /// interface free of WPF types (issue #3225); the concrete implementation expects a
    /// <c>System.Windows.FrameworkElement</c>. Returns <see cref="IPluginTab"/> rather than the
    /// concrete WPF-typed <c>PluginTab</c> so this interface (and its consumers, e.g.
    /// <see cref="Gum.Plugins.BaseClasses.PluginBase"/>) can live in the headless
    /// <c>Gum.Presentation</c> assembly (issue #3950).
    /// </summary>
    IPluginTab AddControl(object element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
    void RemoveTab(IPluginTab plugin);
}
