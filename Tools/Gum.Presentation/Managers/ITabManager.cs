using Gum.Plugins;

namespace Gum.Managers;

public interface ITabManager
{
    /// <summary>
    /// Adds <paramref name="element"/> as a new tab. Typed as <see cref="object"/> to keep this
    /// interface free of UI-framework types (issue #3225): a head shows a control of its own
    /// framework as is, and resolves anything else (a ViewModel) to a view by its own convention.
    /// Returns <see cref="IPluginTab"/> rather than a head's concrete tab type so this interface
    /// (and its consumers, e.g. <see cref="Gum.Plugins.BaseClasses.PluginBase"/>) can live in the
    /// headless <c>Gum.Presentation</c> assembly (issue #3950).
    /// </summary>
    IPluginTab AddControl(object element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom);
    void RemoveTab(IPluginTab plugin);
}
