using System;
using System.Globalization;
using System.Windows.Data;
using Gum.ViewModels;
using WpfWindowState = System.Windows.WindowState;

namespace Gum.Converters;

/// <summary>
/// Converts between the headless <see cref="GumWindowState"/> used on <see cref="MainWindowViewModel"/>
/// (ADR-0004) and the WPF <see cref="WpfWindowState"/> required by <c>Window.WindowState</c>. This is
/// the "thin conversion at the View/binding boundary" for the VM's TwoWay-bound window-chrome state
/// (part of #3856).
/// </summary>
public class GumWindowStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is GumWindowState state
            ? state switch
            {
                GumWindowState.Minimized => WpfWindowState.Minimized,
                GumWindowState.Maximized => WpfWindowState.Maximized,
                _ => WpfWindowState.Normal
            }
            : WpfWindowState.Normal;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is WpfWindowState state
            ? state switch
            {
                WpfWindowState.Minimized => GumWindowState.Minimized,
                WpfWindowState.Maximized => GumWindowState.Maximized,
                _ => GumWindowState.Normal
            }
            : GumWindowState.Normal;
}
