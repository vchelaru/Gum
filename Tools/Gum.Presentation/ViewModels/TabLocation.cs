namespace Gum;

/// <summary>
/// Identifies which region of the main window's docking layout a <c>PluginTab</c> belongs in.
/// </summary>
/// <remarks>
/// Relocated from <c>MainWindow.xaml.cs</c> (part of #3856) — it never had any WPF dependency, it
/// was just physically declared alongside the WPF window class that happened to be in the same
/// file. Pure file-location move: namespace and members are unchanged, so no consumer needed an
/// import change.
/// </remarks>
public enum TabLocation
{
    [System.Obsolete("Use either CenterTop or CenterBottom")]
    Center,
    RightBottom,
    RightTop,
    CenterTop,
    CenterBottom,
    Left
}
