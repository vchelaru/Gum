using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// Adapts an Avalonia <see cref="TextBox"/> for <see cref="TextBoxDisplayLogic"/>: reads and writes
/// its text and paints the value-state background.
/// </summary>
public class AvaloniaDataUiTextBox : IDataUiTextBox
{
    private readonly TextBox _textBox;

    public AvaloniaDataUiTextBox(TextBox textBox)
    {
        _textBox = textBox;
    }

    /// <summary>
    /// Creates the logic for <paramref name="textBox"/> and forwards its focus, key, text, and
    /// double-click events to it. Keys and clicks are handled on the tunnel so they run before the
    /// text box's own handling.
    /// </summary>
    public static TextBoxDisplayLogic CreateLogic(IDataUi container, TextBox textBox)
    {
        TextBoxDisplayLogic logic = new TextBoxDisplayLogic(container, new AvaloniaDataUiTextBox(textBox));

        textBox.GotFocus += (_, _) => logic.HandleGotFocus();
        textBox.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                if (logic.HandleEnterKey())
                {
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                logic.HandleEscapeKey();
            }
        }, RoutingStrategies.Tunnel);
        textBox.TextChanged += (_, _) => logic.HandleTextChanged();
        textBox.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (logic.ShouldSelectAllOnClick(e.ClickCount))
            {
                textBox.SelectAll();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        return logic;
    }

    /// <inheritdoc/>
    public string Text
    {
        get => _textBox.Text ?? string.Empty;
        set => _textBox.Text = value;
    }

    /// <inheritdoc/>
    public void SelectAll() => _textBox.SelectAll();

    /// <inheritdoc/>
    public void ApplyValueState(DataUiValueState state) => DataUiValueStateBrushes.ApplyBackground(_textBox, state);
}
