using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The tool's own Project tab (the head's singleton tree manager and panel) over a temp project,
/// hosted in a headless window with the main window's app-wide hotkeys, and driven the way a user
/// drives it: clicks on rows, right-click menus, key presses, scripted dialogs. Every scenario ends
/// with <see cref="AssertOracles"/>.
/// </summary>
internal sealed class ProjectTreeHarness : IDisposable
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly HeadlessWindowDriver _driver;
    private readonly ToolExceptionWatch _exceptions;
    private readonly Dictionary<AvaloniaPluginTab, bool> _tabVisibilityAtStart;
    private VariableGridHarness? _grid;
    private StatesTabHarness? _states;

    /// <param name="projectFileName">The project's file name; a .gumj name makes a JSON project.</param>
    public ProjectTreeHarness(string projectFileName = "Harness.gumx")
    {
        ToolStartup.EnsureInitialized();
        TreeManager = Services.GetRequiredService<ElementTreeViewManager>();
        View = (AvaloniaElementTreeView)TreeManager.View;
        // The head's tabs outlive the test, and selecting an element with an animation file shows
        // the Animations tab, so Dispose puts each tab back as it was.
        _tabVisibilityAtStart = ((AvaloniaTabManager)Services.GetRequiredService<ITabManager>()).AllTabs
            .ToDictionary(tab => tab, tab => tab.IsVisible);
        Project = new ToolProjectFixture("GumProjectTree", projectFileName);
        _exceptions = new ToolExceptionWatch();
        try
        {
            // Adding elements refuses a project that was never saved.
            SaveAll();
            TreeManager.RefreshUi();
            _driver = new HeadlessWindowDriver((Control)View.Content, width: 400, height: 900, framesFolderName: "GumProjectTree");
            AppWideWindowInput.RouteHotkeys(_driver.Window,
                Services.GetRequiredService<IHotkeyManager>(),
                Services.GetRequiredService<AvaloniaModifierKeyState>());
        }
        catch
        {
            Project.Dispose();
            _exceptions.Dispose();
            throw;
        }
    }

    /// <summary>The temp project, its builders and the scripted dialogs.</summary>
    public ToolProjectFixture Project { get; }

    public ElementTreeViewManager TreeManager { get; }

    public AvaloniaElementTreeView View { get; }

    public HeadlessWindowDriver Input => _driver;

    public ScriptedDialogService Dialogs => Project.Dialogs;

    public ISelectedState SelectedState => Project.SelectedState;

    public IUndoManager UndoManager => Project.UndoManager;

    /// <summary>
    /// The head's Variables tab over the same project, in a window of its own, created on first
    /// use. Select through the tree (<see cref="Click"/>) and undo with <see cref="Undo"/>, as a user does.
    /// </summary>
    public VariableGridHarness Grid => _grid ??= new VariableGridHarness(Project);

    /// <summary>
    /// The head's States tab over the same project, in a window of its own, created on first use.
    /// It follows the element selected through the tree.
    /// </summary>
    public StatesTabHarness States => _states ??= new StatesTabHarness(ThrowIfCrashed);


    #region Finding nodes

    /// <summary>The top-level node named <paramref name="text"/> (Screens, Components, Behaviors).</summary>
    public GumTreeNode RootNode(string text) =>
        View.Nodes.SingleOrDefault(node => node.Text == text)
        ?? throw new InvalidOperationException($"The tree has no top-level {text} node.");

    /// <summary>The node showing <paramref name="element"/>.</summary>
    public GumTreeNode NodeFor(ElementSave element) =>
        TreeManager.GetTreeNodeFor(element) ?? throw new InvalidOperationException($"The tree shows no node for {element.Name}.");

    /// <summary>The node showing <paramref name="instance"/> in its element.</summary>
    public GumTreeNode NodeFor(InstanceSave instance)
    {
        ElementSave owner = instance.ParentContainer ?? throw new InvalidOperationException($"{instance.Name} has no element.");
        return TreeManager.GetTreeNodeFor(instance, NodeFor(owner))
            ?? throw new InvalidOperationException($"The tree shows no node for {owner.Name}.{instance.Name}.");
    }

    /// <summary>The folder node <paramref name="path"/> under <paramref name="root"/>, such as ("Components", "Controls/Buttons").</summary>
    public GumTreeNode FolderNode(string root, string path)
    {
        GumTreeNode node = RootNode(root);
        foreach (string part in path.Split('/'))
        {
            node = node.Nodes.SingleOrDefault(child => child.Tag == null && child.Text == part)
                ?? throw new InvalidOperationException($"The tree has no folder {root}/{path}.");
        }
        return node;
    }

    /// <summary>The names of the nodes under <paramref name="node"/>, in the order the tree shows them.</summary>
    public List<string> ChildTexts(GumTreeNode node) => node.Nodes.Select(child => child.Text).ToList();

    #endregion

    #region Gestures

    /// <summary>Scrolls <paramref name="node"/> into view (expanding its parents) and clicks its row.</summary>
    public void Click(GumTreeNode node, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _driver.Click(RowFor(node), modifiers);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Clicks the expander arrow on <paramref name="node"/>'s row, which toggles it open or closed.</summary>
    public void ClickExpander(GumTreeNode node)
    {
        _driver.Click(((TreeRowView)RowFor(node)).Expander);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Clicks the "Collapse all" button beside the search box.</summary>
    public void ClickCollapseAll() => ClickSearchRowButton(column: 1);

    /// <summary>Clicks the "Collapse to element level" button beside the search box.</summary>
    public void ClickCollapseToElementLevel() => ClickSearchRowButton(column: 2);

    private void ClickSearchRowButton(int column)
    {
        Button button = View.SearchRow.Children.OfType<Button>().Single(candidate => global::Avalonia.Controls.Grid.GetColumn(candidate) == column);
        _driver.Click(button);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>The nodes the tree shows as rows (inside expanded parents), in order.</summary>
    public List<GumTreeNode> VisibleNodes()
    {
        _driver.Layout();
        return View.Tree.VisibleNodes.ToList();
    }

    /// <summary>
    /// Whether <paramref name="node"/>'s row is realized and lies inside the tree's scrolled viewport,
    /// without scrolling to it.
    /// </summary>
    public bool IsScrolledIntoView(GumTreeNode node)
    {
        _driver.Layout();
        ScrollViewer scroller = View.Tree.GetVisualDescendants().OfType<ScrollViewer>().First();
        TreeRowView? row = View.Tree.GetVisualDescendants().OfType<TreeRowView>().SingleOrDefault(candidate => candidate.Row?.Node == node);
        if (row == null || !row.IsEffectivelyVisible || global::Avalonia.VisualExtensions.TranslatePoint(row, default, scroller) is not { } topLeft)
        {
            return false;
        }
        return topLeft.Y >= 0 && topLeft.Y + row.Bounds.Height <= scroller.Viewport.Height;
    }

    /// <summary>
    /// Runs what the tree registers for application exit (it saves the expanded nodes into the
    /// project's user settings), as closing the tool does.
    /// </summary>
    public void RunTreeExitWork()
    {
        List<Action> teardown = new List<Action>();
        ApplicationTeardownMessage message = new ApplicationTeardownMessage(teardown);
        // Only the tree's plugin: the shell's own exit work would write the test app's window layout.
        foreach (IRecipient<ApplicationTeardownMessage> plugin in Services.GetRequiredService<PluginManager>().InitializedPlugins
            .OfType<IRecipient<ApplicationTeardownMessage>>())
        {
            plugin.Receive(message);
        }
        teardown.ShouldNotBeEmpty("the tree's plugin registers exit work");
        foreach (Action action in teardown)
        {
            action();
        }
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Right-clicks <paramref name="node"/>'s row, which selects it and opens the tree's menu.</summary>
    public void RightClick(GumTreeNode node)
    {
        _driver.RightClick(RowFor(node));
        _exceptions.ThrowIfCrashed();
        if (!View.ContextMenu.IsOpen)
        {
            throw new InvalidOperationException($"Right-clicking {node.Text} opened no menu.");
        }
    }

    /// <summary>The headers of the open menu's items (separators excluded).</summary>
    public List<string> MenuHeaders() => View.ContextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? "").ToList();

    /// <summary>
    /// Picks the item at <paramref name="path"/> in the open menu, one header per level ("Add object
    /// to Button", "Sprite"), as a click on it does, and runs the action it posts. A header also
    /// matches the one item that starts with it and a space.
    /// </summary>
    public void PickMenu(params string[] path)
    {
        ContextMenu menu = View.ContextMenu;
        if (!menu.IsOpen)
        {
            throw new InvalidOperationException("No tree menu is open.");
        }
        IEnumerable<object?> items = menu.Items;
        MenuItem? item = null;
        foreach (string header in path)
        {
            List<MenuItem> candidates = items.OfType<MenuItem>().ToList();
            // An exact header, else the one item that starts with it ("Delete" for "Delete Button (Container)").
            item = candidates.SingleOrDefault(candidate => candidate.Header?.ToString() == header)
                ?? (candidates.Count(candidate => candidate.Header?.ToString()?.StartsWith(header + " ", StringComparison.Ordinal) == true) == 1
                    ? candidates.Single(candidate => candidate.Header?.ToString()?.StartsWith(header + " ", StringComparison.Ordinal) == true)
                    : null)
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
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Presses <paramref name="key"/> with the tree focused, as after a click on a row.</summary>
    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        if (!View.Tree.IsKeyboardFocusWithin)
        {
            View.Tree.Focus();
        }
        _driver.Press(key, physicalKey, modifiers);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Types <paramref name="text"/> into the search box, which swaps the tree for the results list.</summary>
    public void Search(string text)
    {
        _driver.TypeInto(View.SearchBox, text);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>Clicks the "Include Variables" option under the search box (shown while the search box has text).</summary>
    public void ClickIncludeVariables()
    {
        _driver.Click(View.IncludeVariablesCheckBox);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>The text of each search result, in order.</summary>
    public List<string> SearchResultTexts() =>
        View.SearchResults.Items.OfType<SearchItemViewModel>().Select(item => item.Display).ToList();

    /// <summary>Clicks the search result showing <paramref name="display"/>, which selects what it stands for.</summary>
    public void ClickSearchResult(string display)
    {
        _driver.Layout();
        Control row = View.SearchResults.GetVisualDescendants().OfType<ListBoxItem>()
            .SingleOrDefault(item => (item.DataContext as SearchItemViewModel)?.Display == display)
            ?? throw new InvalidOperationException($"No search result shows \"{display}\"; they are [{string.Join(", ", SearchResultTexts())}].");
        _driver.Click(row);
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>
    /// Picks the main menu item at <paramref name="path"/> ("File", "New Project"), as the head does
    /// once the menu closes, and runs the work it posts. An item whose action is async (New Project,
    /// Load Recent) may still be running; follow it with <see cref="WaitUntil"/>.
    /// </summary>
    public void PickMainMenu(params string[] path)
    {
        IEnumerable<MenuItemModel> items = Services.GetRequiredService<MenuModel>().TopLevelItems;
        MenuItemModel? item = null;
        foreach (string header in path)
        {
            List<MenuItemModel> candidates = items.Where(candidate => !candidate.IsSeparator).ToList();
            item = candidates.SingleOrDefault(candidate => candidate.Header == header)
                ?? throw new InvalidOperationException($"The menu has no \"{header}\"; it has [{string.Join(", ", candidates.Select(candidate => candidate.Header))}].");
            items = item.Items;
        }
        if (!item!.IsEnabled)
        {
            throw new InvalidOperationException($"The menu item \"{string.Join(" > ", path)}\" is disabled.");
        }
        item.Invoke();
        _driver.Layout();
        _exceptions.ThrowIfCrashed();
    }

    /// <summary>
    /// Pumps the UI thread until <paramref name="condition"/> holds, for work a gesture started
    /// asynchronously (a project load, a theme import); fails after <paramref name="timeout"/>.
    /// </summary>
    public void WaitUntil(Func<bool> condition, TimeSpan timeout, string what)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException($"Waited {timeout.TotalSeconds:0} s for {what}. Messages shown: [{string.Join(" | ", Dialogs.Messages)}].");
            }
            Thread.Sleep(10);
            _driver.Layout();
            _exceptions.ThrowIfCrashed();
        }
        _driver.Layout();
    }

    /// <summary>Ctrl+Z, handled app-wide as in the main window.</summary>
    public void Undo() => Press(Key.Z, PhysicalKey.Z, RawInputModifiers.Control);

    /// <summary>Ctrl+Y, handled app-wide as in the main window.</summary>
    public void Redo() => Press(Key.Y, PhysicalKey.Y, RawInputModifiers.Control);

    #endregion

    /// <summary>Fails at once when a gesture made anywhere in the tool crashed.</summary>
    public void ThrowIfCrashed() => _exceptions.ThrowIfCrashed();

    /// <summary>Everything written to the Output tab since the harness was created.</summary>
    public string OutputWritten => _exceptions.OutputWritten;

    #region Oracles

    /// <summary>Saves every file, as File > Save All does; for setup that edits the data directly.</summary>
    public void SaveAll() => Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);

    /// <summary>The project's files right now, for a later byte comparison.</summary>
    public ProjectFileSnapshot SnapshotFiles() => ProjectFileSnapshot.Take(Project.ProjectFolder);

    /// <summary>
    /// The shared end-of-scenario checks: no exception reached the Output tab, the crash log or the
    /// plugin manager; the tree shows what was saved; the saved project passes <c>gumcli check</c>,
    /// reloads cleanly and re-saves unchanged; and the reloaded tree still matches.
    /// </summary>
    public void AssertOracles()
    {
        _driver.Layout();
        string projectPath = Project.ProjectFilePath;
        ProjectOracles.AssertSaveReloadAndCheckClean(Project,
            checkSaved: () => ProjectOracles.AssertTreeMatchesSavedProject(TreeManager, projectPath));
        _driver.Layout();
        ProjectOracles.AssertTreeMatchesSavedProject(TreeManager, projectPath);
        _exceptions.AssertClean();
    }

    #endregion

    private Control RowFor(GumTreeNode node)
    {
        for (GumTreeNode? parent = node.Parent; parent != null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }
        _driver.Layout();
        View.EnsureVisible(node);
        _driver.Layout();
        return View.Tree.GetVisualDescendants().OfType<TreeRowView>().SingleOrDefault(row => row.Row?.Node == node)
            ?? throw new InvalidOperationException($"The tree realized no row for {node.Text}.");
    }

    public void Dispose()
    {
        try
        {
            // The head's panel outlives the test; a search left in it would hide the next test's tree.
            View.ClearSearchText();
            View.IncludeVariablesCheckBox.IsChecked = false;
            foreach (KeyValuePair<AvaloniaPluginTab, bool> tab in _tabVisibilityAtStart)
            {
                tab.Key.IsVisible = tab.Value;
            }
            _grid?.Dispose();
            _states?.Dispose();
            _driver.Dispose();
        }
        finally
        {
            _exceptions.Dispose();
            Project.Dispose();
            TreeManager.RefreshUi();
        }
    }
}
