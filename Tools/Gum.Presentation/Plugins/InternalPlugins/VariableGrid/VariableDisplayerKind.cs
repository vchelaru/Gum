namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Neutral stand-in for a row's WpfDataUi <c>PreferredDisplayer</c> <see cref="System.Type"/> value,
/// so headless code (<see cref="VariableGridEntry"/>) can express "which control should render this
/// row" without referencing the WPF-coupled WpfDataUi control types directly.
/// </summary>
public enum VariableDisplayerKind
{
    Default,
    ComboBox,
    FileSelection,
    ListBox,
    MultiLineTextBox
}
