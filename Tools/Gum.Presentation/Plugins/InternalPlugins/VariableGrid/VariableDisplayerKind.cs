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
    MultiLineTextBox,
    /// <summary>A button that removes the row's underlying variable rather than editing a value
    /// (the category "common members" remove-from-category row). Maps to the WPF-side
    /// <c>VariableRemoveButton</c> control.</summary>
    RemoveButton
}
