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
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;

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
    private VariableGridHarness? _grid;

    public ProjectTreeHarness()
    {
        ToolStartup.EnsureInitialized();
        TreeManager = Services.GetRequiredService<ElementTreeViewManager>();
        View = (AvaloniaElementTreeView)TreeManager.View;
        Project = new ToolProjectFixture("GumProjectTree");
        _exceptions = new ToolExceptionWatch();
        try
        {
            // Adding elements refuses a project that was never saved.
            SaveAll();
            TreeManager.RefreshUi();
            _driver = new HeadlessWindowDriver((Control)View.Content, width: 400, height: 900, framesFolderName: "GumProjectTree", contentOutlivesTest: true);
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

    /// <summary>Ctrl+Z, handled app-wide as in the main window.</summary>
    public void Undo() => Press(Key.Z, PhysicalKey.Z, RawInputModifiers.Control);

    /// <summary>Ctrl+Y, handled app-wide as in the main window.</summary>
    public void Redo() => Press(Key.Y, PhysicalKey.Y, RawInputModifiers.Control);

    #endregion

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
            _grid?.Dispose();
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
