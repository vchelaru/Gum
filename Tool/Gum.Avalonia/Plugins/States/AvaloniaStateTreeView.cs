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
            [!Button.CommandProperty] = new Binding(nameof(StateTreeViewModel.AddCategoryCommand)),
        }.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        SetDock(addCategory, global::Avalonia.Controls.Dock.Bottom);
        Children.Add(addCategory);
        Children.Add(_tree);
    }

    private static Control BuildRow(StateTreeViewItem? item)
    {
        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };

        TextBlock title = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.TextProperty] = new Binding(nameof(StateTreeViewItem.Title)),
        };
        row.Children.Add(title);

        TextBlock requiredByBehavior = new TextBlock
        {
            Text = "behavior",
            FontSize = 10,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            [!Visual.IsVisibleProperty] = new Binding(nameof(StateTreeViewItem.IsRequiredBySelectedBehavior)),
        }.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
        ToolTip.SetTip(requiredByBehavior, "Required by the selected behavior");
        Grid.SetColumn(requiredByBehavior, 1);
        row.Children.Add(requiredByBehavior);

        if (item is CategoryViewModel)
        {
            Button addState = new Button
            {
                Content = "+",
                Padding = new Thickness(4, 0),
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                [!Button.CommandProperty] = new Binding(nameof(CategoryViewModel.AddStateCommand)),
            }.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
            ToolTip.SetTip(addState, $"Add state to {item.Title}");
            Grid.SetColumn(addState, 2);
            row.Children.Add(addState);
        }
        else if (item is StateViewModel)
        {
            TextBlock edited = new TextBlock
            {
                Text = "✎",
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                [!Visual.IsVisibleProperty] = new Binding(nameof(StateViewModel.IncludesVariablesForSelectedInstance)),
            };
            ToolTip.SetTip(edited, "Sets variables on the selected instance");
            Grid.SetColumn(edited, 2);
            row.Children.Add(edited);
        }

        return row;
    }
}
