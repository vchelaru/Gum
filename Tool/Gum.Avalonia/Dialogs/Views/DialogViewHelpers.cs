using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>View-side glue shared by the dialog views; no decisions live here.</summary>
internal static class DialogViewHelpers
{
    /// <summary>
    /// Double-clicking an item confirms the dialog when it can be confirmed. Twin of the WPF
    /// <c>ListBoxDoubleClick.Command</c> behavior bound to <see cref="DialogViewModel.AffirmativeCommand"/>.
    /// </summary>
    public static void AffirmOnDoubleTap(ListBox listBox)
    {
        listBox.DoubleTapped += (_, e) =>
        {
            if (listBox.DataContext is DialogViewModel viewModel && viewModel.AffirmativeCommand.CanExecute(null))
            {
                viewModel.AffirmativeCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    /// <summary>Focuses <paramref name="textBox"/> and selects its text once the view is shown.</summary>
    public static void FocusAndSelectAllWhenShown(Control view, TextBox textBox)
    {
        view.AttachedToVisualTree += (_, _) =>
        {
            textBox.Focus(NavigationMethod.Tab);
            textBox.SelectAll();
        };
    }

    /// <summary>An item template that shows the item's <paramref name="propertyName"/> as text.</summary>
    public static IDataTemplate TextTemplate(string propertyName) =>
        new FuncDataTemplate<object>((_, _) =>
        {
            TextBlock text = new TextBlock();
            text.Bind(TextBlock.TextProperty, new Binding(propertyName));
            return text;
        });
}
