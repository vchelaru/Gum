using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ImportFromGumxPlugin.Views;

/// <summary>
/// Converts null/empty string → Collapsed, non-empty string → Visible.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts bool true → Visible, false → Collapsed.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Interaction logic for ImportFromGumxView.xaml
/// </summary>
[Dialog(typeof(ImportFromGumxViewModel))]
public partial class ImportFromGumxView : UserControl
{
    public ImportFromGumxView()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Gum Project Files (*.gumx)|*.gumx|All Files (*.*)|*.*",
            Title = "Open Gum Project"
        };

        if (dialog.ShowDialog() == true && DataContext is ImportFromGumxViewModel vm)
        {
            vm.SourcePath = dialog.FileName;
            if (vm.LoadPreviewCommand.CanExecute(null))
            {
                await vm.LoadPreviewCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnSourcePathKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ImportFromGumxViewModel vm
            && vm.LoadPreviewCommand.CanExecute(null))
        {
            await vm.LoadPreviewCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Prevents the user from cycling into the indeterminate state via clicking.
    /// When a checked item is clicked, WPF would set IsChecked = null (indeterminate),
    /// but we intercept and force it to false instead.
    /// This handler fires only on user interaction, not when the VM programmatically sets IsChecked = null.
    /// </summary>
    private void OnCheckBoxIndeterminate(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            checkBox.IsChecked = false;
        }
    }
}
