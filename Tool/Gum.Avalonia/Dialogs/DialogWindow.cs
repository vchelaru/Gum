using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Gum.Avalonia.Themes;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// The window every <see cref="DialogViewModel"/> is shown in, drawn as the WPF <c>DialogWindow</c>:
/// the title as a bold caption inside a rounded frame, the registered view under it, the
/// affirmative and negative buttons underneath, Enter and Escape wired to them (plus the buttons'
/// access keys, alone where the view model opts in), the copy gesture in a message dialog, and the view
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

    /// <summary>
    /// Set false on a dialog view to keep the window from wrapping it in a scroller, so the view gets
    /// the window's bounded height and scrolls a part of itself. Twin of WPF's <c>Dialog.ScrollContent</c>.
    /// </summary>
    public static readonly AttachedProperty<bool> ScrollContentProperty =
        AvaloniaProperty.RegisterAttached<DialogWindow, Control, bool>("ScrollContent", defaultValue: true);

    /// <summary>
    /// Set on a dialog view to open the window at this height while still letting it shrink to fit a
    /// shorter screen, where a fixed <c>Height</c> would be clipped. With <see cref="ScrollContentProperty"/>
    /// false the view scrolls a part of itself; otherwise the whole view scrolls. Twin of WPF's
    /// fixed view height, which <c>Dialog.OnContentChanged</c> clears once the window has sized to it.
    /// </summary>
    public static readonly AttachedProperty<double> PreferredHeightProperty =
        AvaloniaProperty.RegisterAttached<DialogWindow, Control, double>("PreferredHeight", defaultValue: double.NaN);

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

    /// <summary>Gets whether the window scrolls <paramref name="view"/> (the default) or leaves scrolling to it.</summary>
    public static bool GetScrollContent(Control view) => view.GetValue(ScrollContentProperty);

    /// <summary>Set false to give <paramref name="view"/> the window's bounded height instead of a scroller.</summary>
    public static void SetScrollContent(Control view, bool scroll) => view.SetValue(ScrollContentProperty, scroll);

    /// <summary>Gets the height <paramref name="view"/>'s window opens at, or NaN to size to the view.</summary>
    public static double GetPreferredHeight(Control view) => view.GetValue(PreferredHeightProperty);

    /// <summary>Opens <paramref name="view"/>'s window at <paramref name="height"/>, shrinking it on a shorter screen.</summary>
    public static void SetPreferredHeight(Control view, double height) => view.SetValue(PreferredHeightProperty, height);

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

        // As the WPF window: the view scrolls vertically when it would not fit, so the buttons stay
        // reachable under a tall view, unless the view opted out to scroll a part of itself.
        Control scrolledContent = GetScrollContent(content)
            ? new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            }
            : content;
        // Outside the scroller, so a view that scrolls as a whole still scrolls within its preferred height.
        Control viewHost = double.IsNaN(GetPreferredHeight(content))
            ? scrolledContent
            : new PreferredHeightHost(GetPreferredHeight(content)) { Child = scrolledContent };

        DockPanel body = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(footer, Dock.Bottom);
        body.Children.Add(footer);
        body.Children.Add(viewHost);

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
            // As the WPF window: Escape is the negative answer whether or not a Cancel button is shown.
            if (e.Key == Key.Escape && _viewModel.NegativeCommand.CanExecute(null))
            {
                _viewModel.NegativeCommand.Execute(null);
                e.Handled = true;
            }
            // A button's access key alone (Y or N in the delete dialog) answers a dialog that opted in.
            // Alt+letter needs nothing here: the buttons' "_Yes"/"_No" text makes them access keys.
            else if (e.KeyModifiers == KeyModifiers.None && e.Key is >= Key.A and <= Key.Z
                && _viewModel.TryAnswerFromAccessKey((char)('A' + (e.Key - Key.A))))
            {
                e.Handled = true;
            }
        };
        // Tunnel, so the copy gesture reaches the window before a focused button or text block.
        AddHandler(KeyDownEvent, HandleCopyGesture, RoutingStrategies.Tunnel);

        // WPF gives a freshly-opened window keyboard focus to its first focusable control, so the
        // affirmative button's IsDefault fires on Enter with no click needed. Avalonia does not, so a
        // view with nothing else to focus (a Yes/No confirmation, the delete dialog) opened with no
        // keyboard focus at all and Enter/the default button silently did nothing (#4810). Queuing
        // this in the constructor, before the window is ever shown, means it always reaches the front
        // of the Opened invocation list, so a view's own FocusWhenOpened call (registered later, once
        // its control attaches during Show) still wins by running its Focus() second.
        Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            // Skip a disabled OK (Import from .gumx opens with nothing checked), or nothing gets focus.
            Button? fallback = new[] { affirmative, negative }.FirstOrDefault(button => button.IsVisible && button.IsEffectivelyEnabled);
            fallback?.Focus();
        }, DispatcherPriority.Input);
    }

    /// <summary>
    /// Caps the window at the working area of the screen it will open on (the owner's screen, or the
    /// primary one), so a tall view scrolls instead of pushing the buttons off screen. The WPF window
    /// capped itself to the owner's height; the screen is the bound that matters for reachability.
    /// </summary>
    public void FitHeightToScreen(Window? owner)
    {
        Screen? screen = (owner != null ? Screens.ScreenFromWindow(owner) : null) ?? Screens.Primary;
        if (screen != null)
        {
            MaxHeight = screen.WorkingArea.Height / screen.Scaling;
        }
    }

    // As the WPF window: the copy gesture in a message dialog copies the whole message, unless the
    // user selected part of it, which the focused text block copies itself.
    private void HandleCopyGesture(object? sender, KeyEventArgs e)
    {
        if (_viewModel is not MessageDialogViewModel { Message: { Length: > 0 } message } || !IsCopyGesture(e))
        {
            return;
        }
        if (FocusManager?.GetFocusedElement() is SelectableTextBlock { SelectedText.Length: > 0 })
        {
            return;
        }
        _ = Clipboard?.SetTextAsync(message);
        e.Handled = true;
    }

    private bool IsCopyGesture(KeyEventArgs e)
    {
        if (PlatformSettings?.HotkeyConfiguration.Copy is { Count: > 0 } gestures)
        {
            return gestures.Any(gesture => gesture.Matches(e));
        }
        return e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control;
    }

    // Asks for the preferred height, or the height available when that is less, whatever the view's
    // own content would ask for.
    private sealed class PreferredHeightHost : Decorator
    {
        private readonly double _preferredHeight;

        public PreferredHeightHost(double preferredHeight) => _preferredHeight = preferredHeight;

        protected override Size MeasureOverride(Size availableSize)
        {
            double height = Math.Min(_preferredHeight, availableSize.Height);
            return base.MeasureOverride(availableSize.WithHeight(height)).WithHeight(height);
        }
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
