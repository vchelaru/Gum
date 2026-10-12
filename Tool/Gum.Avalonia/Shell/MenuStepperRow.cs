using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The content of a stepper menu row, like the zoom row in Chrome's menu: a label, a minus button,
/// the value, and a plus button. Clicking the value runs the stepper's reset. Placed in a
/// <see cref="MenuItem"/> that stays open on click, so the buttons can be pressed repeatedly.
/// </summary>
public sealed class MenuStepperRow : StackPanel
{
    private readonly MenuStepper _stepper;

    /// <summary>Creates the row for <paramref name="stepper"/> and keeps its value text current.</summary>
    public MenuStepperRow(MenuStepper stepper)
    {
        _stepper = stepper;
        Orientation = Orientation.Horizontal;
        Spacing = 6;

        TextBlock label = new TextBlock
        {
            Text = stepper.Label,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 90,
        };
        MinusButton = CreateButton("−", stepper.DecreaseHeader, stepper.Decrease);
        ValueButton = CreateButton(stepper.ValueText, stepper.ResetHeader, stepper.Reset);
        ValueButton.MinWidth = 52;
        PlusButton = CreateButton("+", stepper.IncreaseHeader, stepper.Increase);

        Children.Add(label);
        Children.Add(MinusButton);
        Children.Add(ValueButton);
        Children.Add(PlusButton);

        stepper.PropertyChanged += OnStepperPropertyChanged;
    }

    /// <summary>The button that lowers the value.</summary>
    public Button MinusButton { get; }

    /// <summary>The button showing the value; clicking it puts the value back to its default.</summary>
    public Button ValueButton { get; }

    /// <summary>The button that raises the value.</summary>
    public Button PlusButton { get; }

    private void OnStepperPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MenuStepper.ValueText))
        {
            ValueButton.Content = _stepper.ValueText;
        }
    }

    private static Button CreateButton(string content, string toolTip, System.Action action)
    {
        Button button = new Button
        {
            Content = content,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(8, 2),
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(button, toolTip);
        // Run immediately: the menu stays open, so nothing has to wait for it to close.
        button.Click += (_, e) =>
        {
            action();
            e.Handled = true;
        };
        return button;
    }
}
