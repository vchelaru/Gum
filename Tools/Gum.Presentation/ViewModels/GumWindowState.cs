namespace Gum.ViewModels;

/// <summary>
/// Mirrors the 3 values of <c>System.Windows.WindowState</c> so <see cref="MainWindowViewModel"/>
/// can expose window-chrome state (ADR-0004) without a WPF type on the property (part of #3856).
/// </summary>
/// <remarks>
/// All 3 values are load-bearing, not just Normal/Maximized: the main window's chrome is
/// <c>TwoWay</c>-bound to this property, so minimizing the real window pushes
/// <see cref="Minimized"/> back into the ViewModel. A 2-state (bool) representation would lose that
/// distinction and cause the binding to re-push <see cref="Normal"/> back down, un-minimizing the
/// window the user just minimized.
/// </remarks>
public enum GumWindowState
{
    Normal,
    Minimized,
    Maximized
}
