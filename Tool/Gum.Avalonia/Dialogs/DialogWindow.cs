using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// The window every <see cref="DialogViewModel"/> is shown in: the registered view on top, the
/// affirmative and negative buttons underneath, Enter and Escape wired to them, and the view
/// model's <see cref="DialogViewModel.RequestClose"/> closing the window.
/// </summary>
public sealed class DialogWindow : Window
{
    /// <summary>
    /// Set on a dialog view to give the window a fixed title. When unset the window binds to the
    /// view model's <c>Title</c> property, falling back to "Gum". Twin of WPF's <c>Dialog.DialogTitle</c>.
    /// </summary>
    public static readonly AttachedProperty<string?> DialogTitleProperty =
        AvaloniaProperty.RegisterAttached<DialogWindow, Control, string?>("DialogTitle");

    /// <summary>
    /// Set on a dialog view to place extra controls (a Browse button, say) at the left of the button
    /// row. Twin of WPF's <c>Dialog.AuxiliaryActions</c>.
    /// </summary>
    public static readonly AttachedProperty<Control?> AuxiliaryActionsProperty =
        AvaloniaProperty.RegisterAttached<DialogWindow, Control, Control?>("AuxiliaryActions");

    private readonly DialogViewModel _viewModel;

    /// <summary>The choice the user made: true affirmative, false negative, null closed otherwise.</summary>
    public bool? Result { get; private set; }

    /// <summary>Gets the title a view asked for, or null.</summary>
    public static string? GetDialogTitle(Control view) => view.GetValue(DialogTitleProperty);

    /// <summary>Gives <paramref name="view"/>'s window a fixed title.</summary>
    public static void SetDialogTitle(Control view, string? title) => view.SetValue(DialogTitleProperty, title);

    /// <summary>Gets the extra button-row controls a view asked for, or null.</summary>
    public static Control? GetAuxiliaryActions(Control view) => view.GetValue(AuxiliaryActionsProperty);

    /// <summary>Places <paramref name="actions"/> at the left of <paramref name="view"/>'s button row.</summary>
    public static void SetAuxiliaryActions(Control view, Control? actions) => view.SetValue(AuxiliaryActionsProperty, actions);

    /// <summary>Builds the window around <paramref name="content"/>, whose DataContext is the view model.</summary>
    public DialogWindow(DialogViewModel viewModel, Control content)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Result = null;

        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 320;
        MaxWidth = 900;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        if (GetDialogTitle(content) is { } fixedTitle)
        {
            Title = fixedTitle;
        }
        else
        {
            this.Bind(TitleProperty, new Binding("Title") { FallbackValue = "Gum" });
        }

        Button affirmative = new Button { MinWidth = 80, IsDefault = true };
        affirmative.Bind(ContentProperty, new Binding(nameof(DialogViewModel.AffirmativeText)));
        affirmative.Bind(Button.CommandProperty, new Binding(nameof(DialogViewModel.AffirmativeCommand)));
        affirmative.IsVisible = viewModel.AffirmativeText != null;

        Button negative = new Button { MinWidth = 80, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };
        negative.Bind(ContentProperty, new Binding(nameof(DialogViewModel.NegativeText)));
        negative.Bind(Button.CommandProperty, new Binding(nameof(DialogViewModel.NegativeCommand)));
        negative.IsVisible = viewModel.NegativeText != null;

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(affirmative);
        buttons.Children.Add(negative);

        DockPanel footer = new DockPanel { Margin = new Thickness(16, 8, 16, 16) };
        if (GetAuxiliaryActions(content) is { } auxiliary)
        {
            DockPanel.SetDock(auxiliary, Dock.Left);
            footer.Children.Add(auxiliary);
        }
        footer.Children.Add(buttons);

        DockPanel root = new DockPanel();
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        root.Children.Add(new Border { Child = content, Margin = new Thickness(16, 16, 16, 0) });
        Content = root;

        viewModel.RequestClose += (_, affirmed) =>
        {
            Result = affirmed;
            Close();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _viewModel.NegativeText != null && _viewModel.NegativeCommand.CanExecute(null))
            {
                _viewModel.NegativeCommand.Execute(null);
                e.Handled = true;
            }
        };
    }
}
