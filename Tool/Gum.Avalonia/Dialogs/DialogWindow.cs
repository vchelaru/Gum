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
    private readonly DialogViewModel _viewModel;

    /// <summary>The choice the user made: true affirmative, false negative, null closed otherwise.</summary>
    public bool? Result { get; private set; }

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
        this.Bind(TitleProperty, new Binding("Title") { FallbackValue = "Gum" });

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
            Margin = new Thickness(16, 8, 16, 16),
        };
        buttons.Children.Add(affirmative);
        buttons.Children.Add(negative);

        DockPanel root = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(new Border { Child = content, Margin = new Thickness(16, 16, 16, 0) });
        Content = root;

        viewModel.RequestClose += (_, affirmed) =>
        {
            Result = affirmed;
            Close();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && viewModel.NegativeText != null && viewModel.NegativeCommand.CanExecute(null))
            {
                viewModel.NegativeCommand.Execute(null);
                e.Handled = true;
            }
        };
    }
}
