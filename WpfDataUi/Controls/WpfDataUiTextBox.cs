using System.Windows.Controls;
using System.Windows.Input;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;

/// <summary>
/// Adapts a WPF <see cref="TextBox"/> for <see cref="TextBoxDisplayLogic"/>: reads and writes its
/// text and paints the value-state background.
/// </summary>
public class WpfDataUiTextBox : IDataUiTextBox
{
    private readonly TextBox _textBox;

    public WpfDataUiTextBox(TextBox textBox)
    {
        _textBox = textBox;
    }

    /// <summary>
    /// Creates the logic for <paramref name="textBox"/> and forwards its focus, key, text, and
    /// double-click events to it.
    /// </summary>
    public static TextBoxDisplayLogic CreateLogic(IDataUi container, TextBox textBox)
    {
        TextBoxDisplayLogic logic = new TextBoxDisplayLogic(container, new WpfDataUiTextBox(textBox));

        textBox.GotFocus += (_, _) => logic.HandleGotFocus();
        textBox.PreviewKeyDown += (_, e) =>
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
        };
        textBox.TextChanged += (_, _) => logic.HandleTextChanged();
        textBox.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (logic.ShouldSelectAllOnClick(e.ClickCount))
            {
                textBox.SelectAll();
                e.Handled = true;
            }
        };

        return logic;
    }

    /// <inheritdoc/>
    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    /// <inheritdoc/>
    public void SelectAll() => _textBox.SelectAll();

    /// <inheritdoc/>
    /// <remarks>
    /// Deferred: <see cref="DataUiGrid.OverridesIsDefaultStylingProperty"/> is inherited, and a
    /// pooled control may not be re-parented into the grid yet when this runs.
    /// </remarks>
    public void ApplyValueState(DataUiValueState state)
    {
        _textBox.Dispatcher.BeginInvoke(() =>
        {
            if (DataUiGrid.GetOverridesIsDefaultStyling(_textBox))
            {
                return;
            }

            if (state == DataUiValueState.Default)
            {
                _textBox.Background = DataUiBrushes.DefaultValueBackground;
            }
            else if (state == DataUiValueState.Indeterminate)
            {
                _textBox.Background = DataUiBrushes.IndeterminateValueBackground;
            }
            else if (_textBox.TryFindResource("Frb.Brushes.Field.Background") != null)
            {
                _textBox.SetResourceReference(TextBox.BackgroundProperty, "Frb.Brushes.Field.Background");
            }
            else
            {
                _textBox.ClearValue(TextBox.BackgroundProperty);
            }
        });
    }
}
