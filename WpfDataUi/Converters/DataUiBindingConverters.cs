using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfDataUi.Converters;

/// <summary>
/// Turns a <see cref="System.Drawing.Color"/> (the neutral <c>MemberCategory.HeaderColor</c>) into
/// a WPF brush; null stays null.
/// </summary>
public class DrawingColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Drawing.Color color)
        {
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Wraps a bound <see cref="ICommand"/> so WPF re-evaluates its CanExecute on
/// <see cref="CommandManager.RequerySuggested"/> (focus and input changes), as well as when the
/// command raises its own CanExecuteChanged. Used for the neutral category menu items.
/// </summary>
public class RequerySuggestedCommandConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ICommand command ? new RequerySuggestedCommand(command) : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private sealed class RequerySuggestedCommand : ICommand
    {
        private readonly ICommand _inner;

        public RequerySuggestedCommand(ICommand inner)
        {
            _inner = inner;
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _inner.CanExecuteChanged += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                _inner.CanExecuteChanged -= value;
            }
        }

        public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);

        public void Execute(object? parameter) => _inner.Execute(parameter);
    }
}
