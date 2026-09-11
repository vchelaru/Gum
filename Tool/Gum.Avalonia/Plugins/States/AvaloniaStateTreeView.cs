using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Themes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using FluentIcons.Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Gum.Avalonia.Plugins.States;

/// <summary>
/// The Avalonia states tree: categories with their states, a "+" on each category to add a state,
/// and a "+ New category" row, bound to the shared <see cref="StateTreeViewModel"/>. Renders the
/// shared right-click menu and passes key presses to the shared <see cref="StateTreeKeyboardHandler"/>.
/// The counterpart of the WPF <c>StateTreeView</c>.
/// </summary>
public sealed class AvaloniaStateTreeView : DockPanel
{
    private readonly IStateTreeViewRightClickService _rightClickService;
    private readonly StateTreeKeyboardHandler _keyboardHandler;
    private readonly global::Avalonia.Controls.TreeView _tree;

    /// <summary>Builds the view over the shared view model, menu and hotkeys.</summary>
    public AvaloniaStateTreeView(StateTreeViewModel viewModel,
        IStateTreeViewRightClickService rightClickService,
        StateTreeKeyboardHandler keyboardHandler)
    {
        _rightClickService = rightClickService;
        _keyboardHandler = keyboardHandler;
        DataContext = viewModel;

        _tree = new global::Avalonia.Controls.TreeView
        {
            ItemTemplate = new FuncTreeDataTemplate<StateTreeViewItem>(
                (item, _) => BuildRow(item),
                item => item is CategoryViewModel category ? category.States : (IEnumerable)Array.Empty<StateViewModel>()),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(StateTreeViewModel.Items)),
        };
        // Expansion and selection live on the item view models, which push selection into the tool.
        _tree.Styles.Add(new Style(x => x.OfType<TreeViewItem>())
        {
            Setters =
            {
                new Setter(TreeViewItem.IsExpandedProperty, new Binding(nameof(StateTreeViewItem.IsExpanded)) { Mode = BindingMode.TwoWay }),
                new Setter(TreeViewItem.IsSelectedProperty, new Binding(nameof(StateTreeViewItem.IsSelected)) { Mode = BindingMode.TwoWay }),
            },
        });
        // Hotkeys get first refusal, ahead of the tree's own arrow-key handling.
        _tree.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (_keyboardHandler.HandleKeyDown(e.ToGumKeyEventArgs()))
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        // Nothing applies to the selection: the menu stays closed rather than opening empty.
        _tree.ContextMenu = AvaloniaContextMenus.CreateRebuildingMenu(() => _rightClickService.MenuItems);

        Button addCategory = new Button
        {
            Content = "+ New category",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Classes = { GumChromeStyles.FlatButtonClass },
            [!Button.CommandProperty] = new Binding(nameof(StateTreeViewModel.AddCategoryCommand)),
        }.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        SetDock(addCategory, global::Avalonia.Controls.Dock.Bottom);
        Children.Add(addCategory);
        Children.Add(_tree);
    }

    // The WPF tree's "BiggerIcon" size: 1.25 times the row's text.
    private static readonly IValueConverter IconSize = GumChromeStyles.ScaleFontSize(1.25);

    // As the WPF States tree: the category or state icon, the title, the required-by-behavior and
    // edits-the-selected-instance markers, and a category's add-state button.
    private static Control BuildRow(StateTreeViewItem? item)
    {
        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto") };

        TextBlock title = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.TextProperty] = new Binding(nameof(StateTreeViewItem.Title)),
        };
        Grid.SetColumn(title, 1);

        row.Children.Add(CreateIcon(title,
            item is CategoryViewModel ? FluentIcons.Common.Icon.DatabaseMultiple : FluentIcons.Common.Icon.Database,
            new Thickness(0, 0, 4, 0), column: 0));
        row.Children.Add(title);

        FluentIcon requiredByBehavior = CreateIcon(title, FluentIcons.Common.Icon.PuzzlePiece, new Thickness(4, 0, 0, 0), column: 2);
        requiredByBehavior.Bind(FluentIcon.ForegroundProperty, new DynamicResourceExtension("Frb.Brushes.Icon.Manilla"));
        requiredByBehavior.Bind(Visual.IsVisibleProperty, new Binding(nameof(StateTreeViewItem.IsRequiredBySelectedBehavior)));
        ToolTip.SetTip(requiredByBehavior, "Required by the selected behavior");
        row.Children.Add(requiredByBehavior);

        if (item is CategoryViewModel)
        {
            Button addState = new Button
            {
                Content = "+",
                Padding = new Thickness(4, 0),
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                Classes = { GumChromeStyles.FlatButtonClass },
                [!Button.CommandProperty] = new Binding(nameof(CategoryViewModel.AddStateCommand)),
            }.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
            ToolTip.SetTip(addState, $"Add state to {item.Title}");
            Grid.SetColumn(addState, 3);
            row.Children.Add(addState);
        }
        else if (item is StateViewModel)
        {
            FluentIcon edited = CreateIcon(title, FluentIcons.Common.Icon.BoxEdit, new Thickness(4, 0, 0, 0), column: 3);
            edited.Bind(Visual.IsVisibleProperty, new Binding(nameof(StateViewModel.IncludesVariablesForSelectedInstance)));
            ToolTip.SetTip(edited, "Sets variables on the selected instance");
            row.Children.Add(edited);
        }

        return row;
    }

    private static FluentIcon CreateIcon(TextBlock title, FluentIcons.Common.Icon icon, Thickness margin, int column)
    {
        FluentIcon result = GumFluentIcons.Create(icon, new Binding(nameof(TextBlock.FontSize)) { Source = title, Converter = IconSize });
        result.Margin = margin;
        result.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(result, column);
        return result;
    }
}
