using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Gum.Avalonia.Themes;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// The window every <see cref="DialogViewModel"/> is shown in, drawn as the WPF <c>DialogWindow</c>:
/// the title as a bold caption inside a rounded frame, the registered view under it, the
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

    /// <summary>Name of the caption text block that shows the title inside the frame.</summary>
    public const string CaptionName = "PART_Caption";

    /// <summary>Name of the affirmative (OK, Yes) button.</summary>
    public const string AffirmativeButtonName = "PART_Affirmative";

    /// <summary>Name of the negative (Cancel, No) button.</summary>
    public const string NegativeButtonName = "PART_Negative";

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

    /// <summary>
    /// Gives <paramref name="target"/> keyboard focus once its dialog window has opened, then runs
    /// <paramref name="afterFocus"/>. Focusing it as it attaches is too early: the window takes
    /// focus as it opens, so the user had to click into the field first.
    /// </summary>
    public static void FocusWhenOpened(Control target, Action? afterFocus = null)
    {
        target.AttachedToVisualTree += HandleAttached;

        void HandleAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            target.AttachedToVisualTree -= HandleAttached;
            if (TopLevel.GetTopLevel(target) is Window window)
            {
                window.Opened += HandleOpened;
            }
        }

        void HandleOpened(object? sender, EventArgs e)
        {
            ((Window)sender!).Opened -= HandleOpened;
            Dispatcher.UIThread.Post(() =>
            {
                target.Focus();
                afterFocus?.Invoke();
            }, DispatcherPriority.Input);
        }
    }

    /// <summary>Builds the window around <paramref name="content"/>, whose DataContext is the view model.</summary>
    public DialogWindow(DialogViewModel viewModel, Control content)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Result = null;

        SizeToContent = SizeToContent.WidthAndHeight;
        MaxWidth = 900;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        // The WPF DialogWindow's chrome: no system frame, a rounded bordered surface, and the title
        // as a bold caption row that drags the window.
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        if (GetDialogTitle(content) is { } fixedTitle)
        {
            Title = fixedTitle;
        }
        else
        {
            this.Bind(TitleProperty, new Binding("Title") { FallbackValue = "Gum" });
        }

        Button affirmative = CreateFooterButton(AffirmativeButtonName, nameof(DialogViewModel.AffirmativeText), nameof(DialogViewModel.AffirmativeCommand));
        affirmative.IsDefault = true;
        affirmative.IsVisible = viewModel.AffirmativeText != null;

        Button negative = CreateFooterButton(NegativeButtonName, nameof(DialogViewModel.NegativeText), nameof(DialogViewModel.NegativeCommand));
        negative.IsCancel = true;
        negative.IsVisible = viewModel.NegativeText != null;

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        buttons.Children.Add(affirmative);
        buttons.Children.Add(negative);

        DockPanel footer = new DockPanel { Margin = new Thickness(0, 16, 0, 0) };
        if (GetAuxiliaryActions(content) is { } auxiliary)
        {
            DockPanel.SetDock(auxiliary, Dock.Left);
            auxiliary.HorizontalAlignment = HorizontalAlignment.Left;
            footer.Children.Add(auxiliary);
        }
        footer.Children.Add(buttons);

        DockPanel body = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(footer, Dock.Bottom);
        body.Children.Add(footer);
        body.Children.Add(content);

        TextBlock caption = new TextBlock
        {
            Name = CaptionName,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        caption.Bind(TextBlock.TextProperty, this.GetObservable(TitleProperty));
        Border captionBar = new Border { Height = 24, Background = Brushes.Transparent, Child = caption };
        captionBar.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(captionBar).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        };
        Border separator = new Border { Height = 1 }.WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Contrast01");

        DockPanel frame = new DockPanel();
        DockPanel.SetDock(captionBar, Dock.Top);
        DockPanel.SetDock(separator, Dock.Top);
        frame.Children.Add(captionBar);
        frame.Children.Add(separator);
        frame.Children.Add(body);
        Content = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Child = frame }
            .WithThemeResource(Border.BackgroundProperty, "Frb.Surface01")
            .WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Border");

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

    // The WPF dialog's action buttons: the default (primary) button, 64 wide at least, 16 by 4 padding.
    private static Button CreateFooterButton(string name, string textPath, string commandPath)
    {
        Button button = new Button { Name = name, MinWidth = 64, Padding = new Thickness(16, 4), Margin = new Thickness(5, 0, 0, 0) };
        button.Bind(ContentProperty, new Binding(textPath));
        button.Bind(Button.CommandProperty, new Binding(commandPath));
        return button;
    }
}
