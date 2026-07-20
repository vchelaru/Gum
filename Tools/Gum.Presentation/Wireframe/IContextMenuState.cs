namespace Gum.Wireframe;

/// <summary>
/// Headless-safe subset of the tool-side <c>IEditingManager</c>: whether the wireframe right-click
/// context menu is currently open. <c>IEditingManager.ContextMenu</c> itself is a WPF
/// <c>System.Windows.Controls.ContextMenu</c>, unreachable from headless <c>Gum.Presentation</c>, but
/// consumers here (e.g. <c>SelectionManager</c>) only ever need this one bit.
/// </summary>
public interface IContextMenuState
{
    bool IsContextMenuOpen { get; }
}
