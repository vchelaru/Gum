namespace Gum.Plugins.InternalPlugins.HideShowTools;

/// <summary>
/// Business logic behind the "Hide Tools"/"Show Tools" menu item, relocated out of the WPF-hosted
/// <c>MainHideShowToolsPlugin</c> (ADR-0005 Phase 3) so it can be unit tested headlessly.
/// </summary>
public class HideShowToolsLogic
{
    private readonly IToolsVisibility _toolsVisibility;

    public HideShowToolsLogic(IToolsVisibility toolsVisibility)
    {
        _toolsVisibility = toolsVisibility;
    }

    /// <summary>
    /// Toggles tool-panel visibility, restoring a sane minimum column/row size when tools are shown
    /// again. Returns the new visibility state.
    /// </summary>
    public bool ToggleToolsVisibility()
    {
        _toolsVisibility.IsToolsVisible = !_toolsVisibility.IsToolsVisible;

        if (_toolsVisibility.IsToolsVisible)
        {
            _toolsVisibility.EnsureMinimumWidth();
        }

        return _toolsVisibility.IsToolsVisible;
    }
}
