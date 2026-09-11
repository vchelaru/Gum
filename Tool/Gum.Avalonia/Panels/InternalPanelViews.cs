using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gum.Avalonia.Controls;
using Gum.Avalonia.Converters;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.Plugins.Errors;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.InternalPlugins.Undos;
using Gum.Plugins.Undos;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Errors tab: one row per error with its code (a help link when there is one), message, and an
/// optional fix-it button; Ctrl+C / Ctrl+Shift+C and the context menu copy. Twin of the WPF
/// <c>ErrorDisplay</c> and <c>ErrorListEntry</c>.
/// </summary>
public sealed class ErrorsView : ListBox
{
    /// <summary>Builds the view.</summary>
    public ErrorsView()
    {
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
        this.Bind(ItemsSourceProperty, new Binding(nameof(AllErrorsViewModel.Errors)));
        this.Bind(SelectedItemProperty, new Binding(nameof(AllErrorsViewModel.SelectedItem)) { Mode = BindingMode.TwoWay });
        ItemTemplate = new FuncDataTemplate<ErrorViewModel>((_, _) => CreateRow());

        MenuItem copy = new MenuItem { Header = "_Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        copy.Bind(MenuItem.CommandProperty, new Binding(nameof(AllErrorsViewModel.CopySelectedErrorCommand)));
        MenuItem copyAll = new MenuItem { Header = "Copy _All Errors", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Shift) };
        copyAll.Bind(MenuItem.CommandProperty, new Binding(nameof(AllErrorsViewModel.CopyAllErrorsCommand)));
        ContextMenu menu = new ContextMenu();
        menu.Items.Add(copy);
        menu.Items.Add(copyAll);
        ContextMenu = menu;

        KeyDown += HandleKeyDown;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not AllErrorsViewModel viewModel || e.Key != Key.C || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }
        System.Windows.Input.ICommand command = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? viewModel.CopyAllErrorsCommand
            : viewModel.CopySelectedErrorCommand;
        if (command.CanExecute(null))
        {
            command.Execute(null);
            e.Handled = true;
        }
    }

    private static Control CreateRow()
    {
        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        TextBlock code = new TextBlock { Margin = new Thickness(4, 0), VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace") };
        code.Bind(TextBlock.TextProperty, new Binding(nameof(ErrorViewModel.Code)));
        code.Bind(IsVisibleProperty, new Binding(nameof(ErrorViewModel.HasCodeWithoutHelpUrl)));
        row.Children.Add(code);

        HyperlinkButton link = new HyperlinkButton { Margin = new Thickness(4, 0), Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace") };
        link.Bind(ContentControl.ContentProperty, new Binding(nameof(ErrorViewModel.Code)));
        link.Bind(IsVisibleProperty, new Binding(nameof(ErrorViewModel.HasHelpUrl)));
        link.Bind(Button.CommandProperty, new Binding("DataContext." + nameof(AllErrorsViewModel.OpenHelpCommand))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ListBox) },
        });
        link.Bind(Button.CommandParameterProperty, new Binding("."));
        ToolTip.SetTip(link, null);
        link.Bind(ToolTip.TipProperty, new Binding(nameof(ErrorViewModel.HelpUrl)));
        row.Children.Add(link);

        TextBlock message = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ErrorViewModel.Message)));
        Grid.SetColumn(message, 1);
        row.Children.Add(message);

        Button action = new Button { Margin = new Thickness(4, 0), Padding = new Thickness(6, 1), VerticalAlignment = VerticalAlignment.Center };
        action.Bind(ContentControl.ContentProperty, new Binding(nameof(ErrorViewModel.ActionName)));
        action.Bind(Button.CommandProperty, new Binding(nameof(ErrorViewModel.ActionCommand)));
        action.Bind(IsVisibleProperty, new Binding(nameof(ErrorViewModel.HasAction)));
        Grid.SetColumn(action, 2);
        row.Children.Add(action);

        return row;
    }
}

/// <summary>The Errors tab header: a green or red dot and the error count. Twin of the WPF <c>ErrorTabHeader</c>.</summary>
public sealed class ErrorTabHeaderView : StackPanel
{
    /// <summary>Builds the header.</summary>
    public ErrorTabHeaderView()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;

        global::Avalonia.Controls.Shapes.Ellipse dot = new global::Avalonia.Controls.Shapes.Ellipse { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center };
        dot.Bind(global::Avalonia.Controls.Shapes.Shape.FillProperty, new Binding(nameof(AllErrorsViewModel.Errors) + ".Count") { Converter = ViewConverters.ErrorCountBrush });
        Children.Add(dot);

        TextBlock count = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        count.Bind(TextBlock.TextProperty, new Binding(nameof(AllErrorsViewModel.CountDescription)));
        Children.Add(count);
    }
}

/// <summary>
/// The History tab: the undo history with entries that would be redone dimmed. The selection follows
/// the undo position, so clicks only scroll. Twin of the WPF <c>UndoDisplay</c>.
/// </summary>
public sealed class UndosView : ListBox
{
    private UndosViewModel? _subscribed;

    /// <summary>Builds the view.</summary>
    public UndosView()
    {
        this.Bind(ItemsSourceProperty, new Binding(nameof(UndosViewModel.HistoryItems)));
        this.Bind(SelectedIndexProperty, new Binding(nameof(UndosViewModel.UndoIndex)) { Mode = BindingMode.OneWay });
        ItemTemplate = new FuncDataTemplate<UndoItemViewModel>((_, _) =>
        {
            TextBlock text = new TextBlock();
            text.Bind(TextBlock.TextProperty, new Binding(nameof(UndoItemViewModel.Display)));
            text.Bind(OpacityProperty, new Binding(nameof(UndoItemViewModel.UndoOrRedo)) { Converter = ViewConverters.RedoOpacity });
            return text;
        });

        // History is read-only: a click must not move the selection (it marks the undo position),
        // but the scroll bar still has to work.
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (e.Source is Visual source && source.FindAncestorOfType<ScrollBar>(includeSelf: true) == null)
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        SelectionChanged += (_, _) =>
        {
            if (SelectedItem != null)
            {
                ScrollIntoView(SelectedItem);
            }
        };
        DataContextChanged += (_, _) => Subscribe(DataContext as UndosViewModel);
    }

    private void Subscribe(UndosViewModel? viewModel)
    {
        if (_subscribed != null)
        {
            _subscribed.FocusCurrentItemRequested -= FocusCurrentItem;
        }
        _subscribed = viewModel;
        if (_subscribed != null)
        {
            _subscribed.FocusCurrentItemRequested += FocusCurrentItem;
        }
    }

    // The tab got focus: select the current history entry and scroll it into view.
    private void FocusCurrentItem()
    {
        if (_subscribed?.CurrentItem is { } current)
        {
            SelectedItem = current;
            ScrollIntoView(current);
        }
    }
}

/// <summary>
/// The Alignment tab: the state banner, a margin, the anchor buttons, and the dock buttons, each
/// forwarding to <see cref="AlignmentViewModel"/>. Icons are glyphs until phase 90 ports the icon
/// set. Twin of the WPF <c>AlignmentPluginControl</c>, <c>AnchorControl</c> and <c>DockControl</c>.
/// </summary>
public sealed class AlignmentView : Grid
{
    /// <summary>Builds the view.</summary>
    public AlignmentView()
    {
        ColumnDefinitions = new ColumnDefinitions("Auto,Auto");
        RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto");

        StateEditingIndicatorBar banner = new StateEditingIndicatorBar();
        SetColumnSpan(banner, 2);
        Children.Add(banner);

        AddLabel("Margin", 1);
        TextBox margin = new TextBox { Margin = new Thickness(4), MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Left };
        margin.Bind(TextBox.TextProperty, new Binding(nameof(AlignmentViewModel.DockMarginText)) { Mode = BindingMode.TwoWay });
        Place(margin, 1);

        AddLabel("Anchor", 2);
        Place(CreateAnchorButtons(), 2);
        Place(CreateMarginNote(), 3);

        AddLabel("Dock", 4);
        Place(CreateDockButtons(), 4);
        Place(CreateMarginNote(), 5);
    }

    private AlignmentViewModel? ViewModel => DataContext as AlignmentViewModel;

    private void AddLabel(string text, int row)
    {
        TextBlock label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0) };
        SetRow(label, row);
        Children.Add(label);
    }

    private void Place(Control control, int row)
    {
        SetRow(control, row);
        SetColumn(control, 1);
        Children.Add(control);
    }

    private static TextBlock CreateMarginNote()
    {
        TextBlock note = new TextBlock { Margin = new Thickness(4, 0, 4, 4), FontStyle = FontStyle.Italic, Opacity = 0.6 };
        note.Bind(TextBlock.TextProperty, new Binding(nameof(AlignmentViewModel.MarginText)));
        note.Bind(IsVisibleProperty, new Binding(nameof(AlignmentViewModel.IsMarginTextVisible)));
        return note;
    }

    private Control CreateAnchorButtons()
    {
        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), Margin = new Thickness(0, 0, 8, 0) };
        AddAt(grid, 0, 0, "↖", "Anchor Top Left", vm => vm.TopLeftButton_Click());
        AddAt(grid, 0, 1, "↑", "Anchor Top", vm => vm.TopButton_Click());
        AddAt(grid, 0, 2, "↗", "Anchor Top Right", vm => vm.TopRightButton_Click());
        AddAt(grid, 1, 0, "←", "Anchor Left", vm => vm.MiddleLeftButton_Click());
        AddAt(grid, 1, 1, "•", "Anchor Center", vm => vm.MiddleMiddleButton_Click());
        AddAt(grid, 1, 2, "→", "Anchor Right", vm => vm.MiddleRightButton_Click());
        AddAt(grid, 2, 0, "↙", "Anchor Bottom Left", vm => vm.BottomLeftButton_Click());
        AddAt(grid, 2, 1, "↓", "Anchor Bottom", vm => vm.BottomMiddleButton_Click());
        AddAt(grid, 2, 2, "↘", "Anchor Bottom Right", vm => vm.BottomRightButton_Click());

        StackPanel extra = new StackPanel();
        extra.Children.Add(CreateButton("↔", "Anchor Center Horizontally", vm => vm.AnchorCenterHorizontally_Click()));
        extra.Children.Add(CreateButton("↕", "Anchor Center Vertically", vm => vm.AnchorCenterVertically_Click()));

        return Row(grid, extra);
    }

    private Control CreateDockButtons()
    {
        // Five buttons in a plus shape; the corners stay empty.
        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), Margin = new Thickness(0, 0, 8, 0) };
        AddAt(grid, 1, 0, "⇤", "Dock Left", vm => vm.DockLeftButton_Click());
        AddAt(grid, 0, 1, "⤒", "Dock Top", vm => vm.DockTopButton_Click());
        AddAt(grid, 1, 2, "⇥", "Dock Right", vm => vm.DockRightButton_Click());
        AddAt(grid, 2, 1, "⤓", "Dock Bottom", vm => vm.DockBottomButton_Click());
        AddAt(grid, 1, 1, "▣", "Fill", vm => vm.DockFillButton_Click());

        StackPanel extra = new StackPanel();
        extra.Children.Add(CreateButton("⬌", "Fill Horizontally", vm => vm.DockFillHorizontallyButton_Click()));
        extra.Children.Add(CreateButton("⬍", "Fill Vertically", vm => vm.DockFillVerticallyButton_Click()));
        extra.Children.Add(CreateButton("⤡", "Size to Children", vm => vm.SizeToChildren_Click()));

        return Row(grid, extra);
    }

    private static StackPanel Row(Control first, Control second)
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };
        row.Children.Add(first);
        row.Children.Add(second);
        return row;
    }

    private void AddAt(Grid grid, int row, int column, string glyph, string tip, Action<AlignmentViewModel> action)
    {
        Button button = CreateButton(glyph, tip, action);
        SetRow(button, row);
        SetColumn(button, column);
        grid.Children.Add(button);
    }

    private Button CreateButton(string glyph, string tip, Action<AlignmentViewModel> action)
    {
        Button button = new Button { Content = glyph, Width = 30, Height = 30, Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(1) };
        ToolTip.SetTip(button, tip);
        button.Click += (_, _) =>
        {
            if (ViewModel is { } viewModel)
            {
                action(viewModel);
            }
        };
        return button;
    }
}

/// <summary>
/// The Behaviors tab: the component's behaviors (missing ones flagged), and an Edit checklist with
/// OK and Cancel, all bound to <see cref="BehaviorsViewModel"/>. Twin of the WPF <c>BehaviorsControl</c>.
/// </summary>
public sealed class BehaviorsView : Grid
{
    /// <summary>Builds the view.</summary>
    public BehaviorsView()
    {
        DockPanel added = new DockPanel();
        added.Bind(IsVisibleProperty, new Binding(nameof(BehaviorsViewModel.IsEditing)) { Converter = BoolConverters.Not });
        Button edit = new Button { Content = "Edit", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        edit.Bind(Button.CommandProperty, new Binding(nameof(BehaviorsViewModel.EditCommand)));
        DockPanel.SetDock(edit, Dock.Bottom);
        added.Children.Add(edit);
        ListBox addedList = new ListBox { ItemTemplate = new FuncDataTemplate<CheckListBehaviorItem>((_, _) => CreateName()) };
        addedList.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(BehaviorsViewModel.AddedBehaviors)));
        addedList.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(BehaviorsViewModel.SelectedBehavior)) { Mode = BindingMode.TwoWay });
        added.Children.Add(addedList);
        Children.Add(added);

        DockPanel editing = new DockPanel();
        editing.Bind(IsVisibleProperty, new Binding(nameof(BehaviorsViewModel.IsEditing)));
        Grid buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Button ok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        ok.Bind(Button.CommandProperty, new Binding(nameof(BehaviorsViewModel.ConfirmEditCommand)));
        buttons.Children.Add(ok);
        Button cancel = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        cancel.Bind(Button.CommandProperty, new Binding(nameof(BehaviorsViewModel.CancelEditCommand)));
        SetColumn(cancel, 1);
        buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Bottom);
        editing.Children.Add(buttons);
        ListBox all = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<CheckListBehaviorItem>((_, _) =>
            {
                CheckBox box = new CheckBox { Content = CreateName() };
                box.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(CheckListBehaviorItem.IsChecked)) { Mode = BindingMode.TwoWay });
                return box;
            }),
        };
        all.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(BehaviorsViewModel.AllBehaviors)));
        editing.Children.Add(all);
        Children.Add(editing);
    }

    private static TextBlock CreateName()
    {
        TextBlock name = new TextBlock();
        name.Bind(TextBlock.TextProperty, new Binding(nameof(CheckListBehaviorItem.DisplayText)));
        name.Bind(TextBlock.ForegroundProperty, new Binding(nameof(CheckListBehaviorItem.IsOrphaned)) { Converter = ViewConverters.OrphanForeground });
        name.Bind(TextBlock.FontStyleProperty, new Binding(nameof(CheckListBehaviorItem.IsOrphaned)) { Converter = ViewConverters.OrphanFontStyle });
        return name;
    }
}
