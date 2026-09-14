using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;

/// <summary>
/// The text field a <see cref="TextBoxDisplayLogic"/> edits. Each head adapts its own text box and
/// forwards focus, key, and text-changed events to the logic.
/// </summary>
public interface IDataUiTextBox
{
    /// <summary>The field's text.</summary>
    string Text { get; set; }

    /// <summary>Selects all of the field's text.</summary>
    void SelectAll();

    /// <summary>Tints the field for <paramref name="state"/>, unless the grid styles defaults itself.</summary>
    void ApplyValueState(DataUiValueState state);
}
