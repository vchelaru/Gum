namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Neutral mirror of <c>WpfDataUi.DataTypes.SetPropertyCommitType</c>, so headless code
/// (<see cref="VariableGridEntry"/>) can distinguish a still-in-progress edit (e.g. dragging a
/// slider) from a finalized commit without referencing the WPF-coupled WpfDataUi assembly.
/// </summary>
public enum VariablePropertyCommitType
{
    /// <summary>A value is being set, but the user is continually editing it (e.g. dragging a slider).</summary>
    Intermediate,

    /// <summary>A value has been fully set (e.g. finished dragging a slider, or a text box lost focus).</summary>
    Full
}
