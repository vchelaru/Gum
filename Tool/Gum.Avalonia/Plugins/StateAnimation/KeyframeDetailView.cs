using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using FlatRedBall.Glue.StateInterpolation;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.StateAnimation;

/// <summary>
/// The selected keyframe's details: its state (editable, so a renamed or deleted state is not coerced
/// away, issue #3386), time, and the interpolation to the next state. Twin of the WPF <c>StateView</c>.
/// </summary>
public sealed class KeyframeDetailView : ScrollViewer
{
    // A ScrollViewer subclass gets no theme (styles match the exact type) and so draws nothing.
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    /// <summary>Builds the view; its DataContext is an <see cref="AnimatedKeyframeViewModel"/>.</summary>
    public KeyframeDetailView()
    {
        Padding = new Thickness(4);
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto") };

        TextBlock missing = new TextBlock { FontWeight = FontWeight.Bold, Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2) };
        missing.Bind(TextBlock.TextProperty, new Binding(nameof(AnimatedKeyframeViewModel.MissingReferenceMessage)) { FallbackValue = "Could not find state or animation" });
        missing.Bind(IsVisibleProperty, new Binding(nameof(AnimatedKeyframeViewModel.ShowInvalidStateWarning)) { FallbackValue = false });
        Place(grid, missing, 0, 0, span: 2);

        ComboBox state = new ComboBox { IsEditable = true, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 1) };
        state.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(AnimatedKeyframeViewModel.AvailableStates)));
        state.Bind(ComboBox.TextProperty, new Binding(nameof(AnimatedKeyframeViewModel.StateName)) { Mode = BindingMode.TwoWay });
        state.Bind(IsVisibleProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsStateComboBoxVisible)));
        Place(grid, state, 1, 0, span: 2);

        Place(grid, Label("Time"), 2, 0);
        TextBox time = new TextBox { Margin = new Thickness(0, 1), HorizontalContentAlignment = HorizontalAlignment.Center };
        time.Bind(TextBox.TextProperty, new Binding(nameof(AnimatedKeyframeViewModel.Time)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        // Enter applies the typed time without leaving the box.
        time.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                BindingOperations.GetBindingExpressionBase(time, TextBox.TextProperty)?.UpdateSource();
                e.Handled = true;
            }
        };
        Place(grid, time, 2, 1);

        TextBlock interpolationLabel = Label("Interpolation Type");
        BindInterpolationVisible(interpolationLabel);
        Place(grid, interpolationLabel, 3, 0);
        ComboBox interpolation = new ComboBox { ItemsSource = Enum.GetValues<InterpolationType>(), HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 1) };
        interpolation.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(AnimatedKeyframeViewModel.InterpolationType)) { Mode = BindingMode.TwoWay });
        BindInterpolationVisible(interpolation);
        Place(grid, interpolation, 3, 1);

        TextBlock easingLabel = Label("In/Out");
        BindInterpolationVisible(easingLabel);
        Place(grid, easingLabel, 4, 0);
        ComboBox easing = new ComboBox { ItemsSource = Enum.GetValues<Easing>(), HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 1) };
        easing.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(AnimatedKeyframeViewModel.Easing)) { Mode = BindingMode.TwoWay });
        BindInterpolationVisible(easing);
        Place(grid, easing, 4, 1);

        TextBlock caption = new TextBlock
        {
            Text = "Interpolation values apply between this state and the next state.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 1),
        };
        Place(grid, caption, 5, 0, span: 2);

        Content = grid;
    }

    private static TextBlock Label(string text) =>
        new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };

    private static void BindInterpolationVisible(Control control) =>
        control.Bind(IsVisibleProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsInterpolationElementVisible)));

    private static void Place(Grid grid, Control control, int row, int column, int span = 1)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        Grid.SetColumnSpan(control, span);
        grid.Children.Add(control);
    }
}
