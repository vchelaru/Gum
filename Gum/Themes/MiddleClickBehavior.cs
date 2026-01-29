using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatRedBall.Glue.Themes;

/// <summary>
/// Provides behavior for executing a command when the middle mouse button is clicked on a <see cref="UIElement"/>.
/// </summary>
public static class MiddleClickBehavior
{
    public static readonly DependencyProperty MiddleClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "MiddleClickCommand",
            typeof(ICommand),
            typeof(MiddleClickBehavior),
            new UIPropertyMetadata(null, OnMiddleClickCommandChanged));

    public static void SetMiddleClickCommand(UIElement target, ICommand value)
    {
        target.SetValue(MiddleClickCommandProperty, value);
    }

    public static ICommand GetMiddleClickCommand(UIElement target)
    {
        return (ICommand)target.GetValue(MiddleClickCommandProperty);
    }

    private static void OnMiddleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement tabItem)
        {
            return;
        }

        switch (e)
        {
            case { OldValue: null, NewValue: not null }:
                tabItem.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown), true);
                break;
            case { OldValue: not null, NewValue: null }:
                tabItem.RemoveHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
                break;
        }
    }

    private static void OnMouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed &&
            sender is UIElement element &&
            GetMiddleClickCommand(element) is {} command &&
            command.CanExecute(element))
        {
            command.Execute(element);
            e.Handled = true;
        }
    }
}
