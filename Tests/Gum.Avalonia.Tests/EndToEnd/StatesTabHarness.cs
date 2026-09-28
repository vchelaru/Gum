using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentIcons.Avalonia;
using Gum.Avalonia.Plugins.States;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The head's own States tab (the singleton state tree view) in a headless window with the main
/// window's app-wide hotkeys, over the project a <see cref="ProjectTreeHarness"/> owns. Rows are
/// clicked and right-clicked, the "+" buttons pressed and keys sent with the tree focused.
/// </summary>
internal sealed class StatesTabHarness : IDisposable
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly HeadlessWindowDriver _driver;
    private readonly Action _throwIfCrashed;

    public StatesTabHarness(Action throwIfCrashed)
    {
        _throwIfCrashed = throwIfCrashed;
        View = ((AvaloniaTabManager)Services.GetRequiredService<ITabManager>()).AllTabs
            .Select(tab => tab.Content).OfType<AvaloniaStateTreeView>().Single();
        _driver = new HeadlessWindowDriver(View, width: 360, height: 700, framesFolderName: "GumStatesTab");
        AppWideWindowInput.RouteHotkeys(_driver.Window,
            Services.GetRequiredService<IHotkeyManager>(),
            Services.GetRequiredService<AvaloniaModifierKeyState>());
    }

    public AvaloniaStateTreeView View { get; }

    public HeadlessWindowDriver Input => _driver;

    public TreeView Tree => View.GetVisualDescendants().OfType<TreeView>().Single();

    public StateTreeViewModel ViewModel => (StateTreeViewModel)View.DataContext!;

    #region Reading the tab

    /// <summary>Each category the tab shows with its states, in order: "Looks: Pressed, Hover".</summary>
    public List<string> Shown()
    {
        _driver.Layout();
        return ViewModel.Categories
            .Select(category => $"{category.Title}: {string.Join(", ", category.States.Select(state => state.Title))}")
            .ToList();
    }

    /// <summary>The row item showing <paramref name="state"/>, found by name so an undo's copy still matches.</summary>
    public StateViewModel ItemFor(string categoryName, string stateName) =>
        ViewModel.Categories.SingleOrDefault(category => category.Data.Name == categoryName)?.States.SingleOrDefault(state => state.Data.Name == stateName)
        ?? throw new InvalidOperationException($"The States tab shows no {categoryName}/{stateName}; it shows [{string.Join(" | ", Shown())}].");

    /// <summary>The row item showing the category named <paramref name="categoryName"/>.</summary>
    public CategoryViewModel ItemFor(string categoryName) =>
        ViewModel.Categories.SingleOrDefault(category => category.Data.Name == categoryName)
        ?? throw new InvalidOperationException($"The States tab shows no category {categoryName}; it shows [{string.Join(" | ", Shown())}].");

    /// <summary>Whether <paramref name="item"/>'s row shows the edited-state (sets variables on the selection) marker.</summary>
    public bool ShowsEditedMarker(StateTreeViewItem item) => MarkerVisible(item, "Sets variables on the selected instance");

    /// <summary>Whether <paramref name="item"/>'s row shows the required-by-behavior marker.</summary>
    public bool ShowsBehaviorMarker(StateTreeViewItem item) => MarkerVisible(item, "Required by the selected behavior");

    private bool MarkerVisible(StateTreeViewItem item, string tip) =>
        HeaderOf(item).GetVisualDescendants().OfType<FluentIcon>()
            .Single(icon => ToolTip.GetTip(icon) as string == tip).IsVisible;

    #endregion

    #region Gestures

    /// <summary>Clicks <paramref name="item"/>'s row, which selects it in the tool.</summary>
    public void Click(StateTreeViewItem item)
    {
        _driver.Click(TitleOf(item));
        _throwIfCrashed();
    }

    /// <summary>Right-clicks <paramref name="item"/>'s row, which selects it and opens the tab's menu.</summary>
    public void RightClick(StateTreeViewItem item)
    {
        _driver.RightClick(TitleOf(item));
        _throwIfCrashed();
        if (Tree.ContextMenu?.IsOpen != true)
        {
            throw new InvalidOperationException($"Right-clicking {item.Title} opened no menu.");
        }
    }

    /// <summary>The headers of the open menu's items (separators excluded).</summary>
    public List<string> MenuHeaders() => Tree.ContextMenu!.Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? "").ToList();

    /// <summary>Picks the item at <paramref name="path"/> in the open menu, one header per level, as a click does.</summary>
    public void PickMenu(params string[] path)
    {
        ContextMenu menu = Tree.ContextMenu ?? throw new InvalidOperationException("The States tab has no menu.");
        if (!menu.IsOpen)
        {
            throw new InvalidOperationException("No States tab menu is open.");
        }
        IEnumerable<object?> items = menu.Items;
        MenuItem? item = null;
        foreach (string header in path)
        {
            List<MenuItem> candidates = items.OfType<MenuItem>().ToList();
            item = candidates.SingleOrDefault(candidate => candidate.Header?.ToString() == header)
                ?? throw new InvalidOperationException($"The menu has no \"{header}\"; it has [{string.Join(", ", candidates.Select(candidate => candidate.Header))}].");
            items = item.Items;
        }
        if (!item!.IsEnabled)
        {
            throw new InvalidOperationException($"The menu item \"{string.Join(" > ", path)}\" is disabled.");
        }
        // The menu's popup is its own top level, which the window's pointer input does not reach.
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        menu.Close();
        _driver.Layout();
        _throwIfCrashed();
    }

    /// <summary>Clicks "+ New category" under the tree.</summary>
    public void ClickNewCategory()
    {
        _driver.Click(View.Children.OfType<Button>().Single(button => button.Content as string == "+ New category"));
        _throwIfCrashed();
    }

    /// <summary>Clicks the "+" on <paramref name="category"/>'s row.</summary>
    public void ClickAddState(CategoryViewModel category)
    {
        _driver.Click(HeaderOf(category).GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "+"));
        _throwIfCrashed();
    }

    /// <summary>Presses <paramref name="key"/> with the selected row focused, as after a click on it.</summary>
    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _driver.Layout();
        TreeViewItem? selected = Tree.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(row => row.IsSelected);
        if (selected != null)
        {
            selected.Focus();
        }
        else
        {
            Tree.Focus();
        }
        _driver.Press(key, physicalKey, modifiers);
        _throwIfCrashed();
    }

    #endregion

    private Grid HeaderOf(StateTreeViewItem item)
    {
        if (item is StateViewModel && ViewModel.Categories.FirstOrDefault(category => category.States.Contains(item)) is { } owner)
        {
            owner.IsExpanded = true;
        }
        _driver.Layout();
        return Tree.GetVisualDescendants().OfType<Grid>()
            .FirstOrDefault(grid => grid.DataContext == item && grid.GetVisualParent() is not TreeViewItem && grid.Children.OfType<TextBlock>().Any())
            ?? throw new InvalidOperationException($"The States tab realized no row for {item.Title}.");
    }

    private TextBlock TitleOf(StateTreeViewItem item) =>
        HeaderOf(item).Children.OfType<TextBlock>().Single();

    public void Dispose() => _driver.Dispose();
}
