using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImportFromGumxPlugin.Views;

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

    private async void OnSourcePathKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ImportFromGumxViewModel vm
            && vm.LoadPreviewCommand.CanExecute(null))
        {
            e.Handled = true;
            await vm.LoadPreviewCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Ensures the CheckBox displays the correct initial state. Uses SetCurrentValue so the
    /// OneWay binding remains active for future VM property changes.
    /// </summary>
    private void OnCheckBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is ImportTreeNodeViewModel vm)
            cb.SetCurrentValue(CheckBox.IsCheckedProperty, vm.IsChecked);
    }

    /// <summary>
    /// Drives all VM updates from user clicks. The IsChecked binding is Mode=OneWay, so WPF
    /// never writes back through the binding. With IsThreeState=false (the default), WPF's
    /// built-in toggle is !IsChecked.GetValueOrDefault(), which naturally maps:
    ///   unchecked (false) → true,  checked (true) → false,  indeterminate (null) → true.
    /// </summary>
    private void OnCheckBoxClick(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ImportTreeNodeViewModel vm)
        {
            vm.IsChecked = checkBox.IsChecked;
        }
    }
}
