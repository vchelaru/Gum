using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Menus;
using Gum.ViewModels;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on moving around the Project tree (inventory area TREE, and the tree's
/// hotkeys): selecting with clicks and keys, expanding and collapsing, the icons, the menus that
/// show a node's file, importing, and the tree keeping up with changes made elsewhere. Each ends
/// with the shared oracles (<see cref="ProjectTreeHarness.AssertOracles"/>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class TreeNavigationScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    #region Selecting

    [AvaloniaFact]
    [Trait("Feature", "TREE-001")]
    [Trait("Feature", "TREE-003")]
    [Trait("Feature", "TREE-004")]
    [Trait("Feature", "TREE-012")]
    public void ClicksShiftClicksAndArrowKeys_SelectNodes_ShowingEachKindsIcon_WithoutChangingAFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave title = tree.Project.AddScreen("Title");
        ComponentSave button = tree.Project.AddComponent("Controls/Button");
        InstanceSave a = tree.Project.AddInstance(button, "A", "Rectangle");
        // A Text would report its font file missing: the tests generate no fonts.
        InstanceSave b = tree.Project.AddInstance(button, "B", "NineSlice");
        InstanceSave c = tree.Project.AddInstance(button, "C", "Sprite");
        InstanceSave d = tree.Project.AddInstance(button, "D", "Container");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Controls/Button");
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.NodeFor(title).ImageIndex.ShouldBe(TreeNodeImageIndices.ScreenImageIndex);
        tree.NodeFor(button).ImageIndex.ShouldBe(TreeNodeImageIndices.ComponentImageIndex);
        tree.FolderNode("Components", "Controls").ImageIndex.ShouldBe(TreeNodeImageIndices.FolderImageIndex);
        tree.RootNode("Behaviors").Nodes.Single().ImageIndex.ShouldBe(TreeNodeImageIndices.BehaviorImageIndex);
        new[] { a, b, c, d, okButton }.Select(instance => tree.NodeFor(instance).ImageIndex).ShouldBe(new[]
        {
            TreeNodeImageIndices.RectangleInstanceImageIndex,
            TreeNodeImageIndices.NineSliceInstanceImageIndex,
            TreeNodeImageIndices.SpriteInstanceImageIndex,
            TreeNodeImageIndices.ContainerInstanceImageIndex,
            TreeNodeImageIndices.InstanceImageIndex,
        });

        // A click selects one node, and the next click moves the selection.
        tree.Click(tree.NodeFor(title));
        tree.SelectedState.SelectedScreen.ShouldBeSameAs(title);
        tree.NodeFor(title).IsSelected.ShouldBeTrue();
        tree.Click(tree.NodeFor(button));
        tree.SelectedState.SelectedElement.ShouldBeSameAs(button);
        tree.SelectedState.SelectedInstance.ShouldBeNull();
        tree.NodeFor(title).IsSelected.ShouldBeFalse();

        // Shift+click selects the range from the last clicked node.
        tree.Click(tree.NodeFor(a));
        tree.Click(tree.NodeFor(c), RawInputModifiers.Shift);
        tree.SelectedState.SelectedInstances.ShouldBe(new[] { a, b, c }, ignoreOrder: true);
        new[] { a, b, c, d }.Select(instance => tree.NodeFor(instance).IsSelected).ShouldBe(new[] { true, true, true, false });

        // The arrows walk the rows and open and close nodes; in the tree they never nudge.
        tree.Click(tree.NodeFor(b));
        tree.Press(Key.Down, PhysicalKey.ArrowDown);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(c);
        tree.Press(Key.Down, PhysicalKey.ArrowDown);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(d);
        tree.Press(Key.Up, PhysicalKey.ArrowUp);
        tree.SelectedState.SelectedInstances.ShouldBe(new[] { c });
        tree.NodeFor(c).IsSelected.ShouldBeTrue();
        tree.NodeFor(d).IsSelected.ShouldBeFalse();
        tree.Press(Key.Left, PhysicalKey.ArrowLeft);
        tree.SelectedState.SelectedInstance.ShouldBeNull();
        tree.SelectedState.SelectedElement.ShouldBeSameAs(button);
        tree.Press(Key.Left, PhysicalKey.ArrowLeft);
        tree.NodeFor(button).IsExpanded.ShouldBeFalse();
        tree.VisibleNodes().ShouldNotContain(tree.NodeFor(a));
        tree.Press(Key.Right, PhysicalKey.ArrowRight);
        tree.NodeFor(button).IsExpanded.ShouldBeTrue();
        tree.Press(Key.Right, PhysicalKey.ArrowRight);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(a);

        tree.SnapshotFiles().ShouldMatch(start, "selecting and walking the tree changes no file");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-005")]
    [Trait("Feature", "TREE-006")]
    public void ExpanderAndCollapseButtons_OpenAndCloseNodes_AndTheOpenNodesSurviveClosingAndReopening()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "Background", "Rectangle");
        ComponentSave toggle = tree.Project.AddComponent("Controls/Toggle");
        tree.Project.AddInstance(toggle, "Check", "Sprite");
        ComponentSave panel = tree.Project.AddComponent("Panel");
        tree.Project.AddInstance(panel, "Frame", "NineSlice");
        tree.Click(tree.NodeFor(panel));
        GumTreeNode components = tree.RootNode("Components");

        // The expander toggles a node without changing the selection.
        tree.ClickExpander(components);
        components.IsExpanded.ShouldBeFalse();
        tree.VisibleNodes().ShouldNotContain(tree.NodeFor(panel));
        tree.ClickExpander(components);
        components.IsExpanded.ShouldBeTrue();
        tree.VisibleNodes().ShouldContain(tree.NodeFor(panel));
        tree.SelectedState.SelectedElement.ShouldBeSameAs(panel);

        // Collapse all closes every node; a second click brings back what was open.
        tree.NodeFor(toggle).IsExpanded = true;
        tree.NodeFor(panel).IsExpanded = true;
        tree.ClickCollapseAll();
        new[] { tree.RootNode("Screens"), components, tree.FolderNode("Components", "Controls"), tree.NodeFor(panel) }
            .ShouldAllBe(node => !node.IsExpanded);
        tree.ClickCollapseAll();
        new[] { components, tree.FolderNode("Components", "Controls"), tree.NodeFor(toggle), tree.NodeFor(panel) }
            .ShouldAllBe(node => node.IsExpanded);

        // Collapse to element level closes the elements and keeps the folders open.
        tree.ClickCollapseToElementLevel();
        components.IsExpanded.ShouldBeTrue();
        tree.FolderNode("Components", "Controls").IsExpanded.ShouldBeTrue();
        tree.NodeFor(toggle).IsExpanded.ShouldBeFalse();
        tree.NodeFor(panel).IsExpanded.ShouldBeFalse();

        // Screens closed, Toggle open: closing the tool saves that, and reopening the project restores it.
        tree.ClickExpander(tree.RootNode("Screens"));
        tree.ClickExpander(tree.NodeFor(toggle));
        tree.RunTreeExitWork();
        tree.Project.SaveAndReload();
        tree.Input.Layout();

        tree.RootNode("Screens").IsExpanded.ShouldBeFalse();
        tree.RootNode("Components").IsExpanded.ShouldBeTrue();
        tree.FolderNode("Components", "Controls").IsExpanded.ShouldBeTrue();
        tree.NodeFor(Component(tree, "Controls/Toggle")).IsExpanded.ShouldBeTrue();
        tree.NodeFor(Component(tree, "Panel")).IsExpanded.ShouldBeFalse();

        tree.AssertOracles();
    }

    #endregion

    #region Showing files

    [AvaloniaFact]
    [Trait("Feature", "TREE-013")]
    [Trait("Feature", "TREE-014")]
    [Trait("Feature", "TREE-022")]
    [Trait("Feature", "TREE-026")]
    [Trait("Feature", "TREE-040")]
    public void ViewInExplorerAndCopyFullPath_OnEachKindOfNode_ShowAndCopyItsFileOrFolder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        RecordingFileSystemRevealService reveal = (RecordingFileSystemRevealService)Services.GetRequiredService<IFileSystemRevealService>();
        RecordingClipboardService clipboard = (RecordingClipboardService)Services.GetRequiredService<IClipboardService>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        Menus.MenuItemModel palette = Services.GetRequiredService<Menus.MenuModel>().GetItem("View")!.Items.Single(item => item.Header == "Standards palette");
        bool? originalPalette = projectManager.UseStandardsPalette;
        bool originalEffectivePalette = projectManager.EffectiveUseStandardsPalette;
        reveal.Clear();
        clipboard.Clear();
        string folder = tree.Project.ProjectFolder;
        ComponentSave toggle = tree.Project.AddComponent("Controls/Toggle");

        // An element: its file.
        tree.RightClick(tree.NodeFor(toggle));
        tree.PickMenu("Copy Full Path");
        new FilePath(clipboard.LastText!).ShouldBe(new FilePath(Path.Combine(folder, "Components", "Controls", "Toggle.gucx")));
        tree.RightClick(tree.NodeFor(toggle));
        tree.PickMenu("View in explorer");
        LastRequest(reveal).ShouldBe(("Reveal", new FilePath(Path.Combine(folder, "Components", "Controls", "Toggle.gucx"))));

        // A folder and a top folder: the folder itself.
        tree.RightClick(tree.FolderNode("Components", "Controls"));
        tree.PickMenu("View in explorer");
        LastRequest(reveal).ShouldBe(("OpenFolder", new FilePath(Path.Combine(folder, "Components", "Controls") + "/")));
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("View in explorer");
        LastRequest(reveal).ShouldBe(("OpenFolder", new FilePath(Path.Combine(folder, "Screens") + "/")));
        Directory.Exists(Path.Combine(folder, "Screens")).ShouldBeTrue("the Screens folder is made on the way");

        // A standard element (in the tree while the Standards palette is off): its file, and Force Save writes it again.
        try
        {
            if (palette.IsChecked)
            {
                tree.PickMainMenu("View", "Standards palette");
            }
            tree.Input.Layout();
            GumTreeNode sprite = tree.RootNode("Standard").Nodes.Single(node => node.Text == "Sprite");
            string spriteFile = Path.Combine(folder, "Standards", "Sprite.gutx");
            tree.RightClick(sprite);
            tree.MenuHeaders().ShouldBe(new[] { "View in explorer", "Force Save Object" });
            tree.PickMenu("View in explorer");
            LastRequest(reveal).ShouldBe(("Reveal", new FilePath(spriteFile)));
            string spriteText = File.ReadAllText(spriteFile);
            File.Delete(spriteFile);
            tree.RightClick(sprite);
            tree.PickMenu("Force Save Object");
            File.ReadAllText(spriteFile).ShouldBe(spriteText);
        }
        finally
        {
            if (palette.IsChecked != originalEffectivePalette)
            {
                tree.PickMainMenu("View", "Standards palette");
            }
            projectManager.UseStandardsPalette = originalPalette;
            projectManager.SaveGeneralSettings();
        }

        // The project title above the panels: the project file. A headless main window that is never
        // shown opens no popup, so the menu is built from the title's own path, as its Opening does.
        Gum.Avalonia.Shell.MainWindow window = Services.GetRequiredService<Gum.Avalonia.Shell.MainWindow>();
        Gum.Avalonia.Shell.ShellViewModel shell = (Gum.Avalonia.Shell.ShellViewModel)window.DataContext!;
        global::Avalonia.Controls.ToolTip.GetTip(ProjectTitle(window)).ShouldBe(shell.ProjectFilePath);
        new FilePath(shell.ProjectFilePath!).ShouldBe(new FilePath(tree.Project.ProjectFilePath));
        List<ContextMenuItemViewModel> titleItems = ProjectTitleContextMenuBuilder.Build(shell.ProjectFilePath, reveal, clipboard);
        titleItems.Select(item => item.Text).ShouldBe(new[] { "View in explorer", "Copy full path" });
        titleItems.ShouldAllBe(item => item.IsEnabled);
        titleItems[1].Action!();
        new FilePath(clipboard.LastText!).ShouldBe(new FilePath(tree.Project.ProjectFilePath));
        titleItems[0].Action!();
        LastRequest(reveal).ShouldBe(("Reveal", new FilePath(tree.Project.ProjectFilePath)));

        tree.AssertOracles();
    }

    #endregion

    #region Importing

    [AvaloniaFact]
    [Trait("Feature", "TREE-016")]
    [Trait("Feature", "TREE-021")]
    public void ImportScreenAndImportBehavior_AddFilesTheProjectDidNotList()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string folder = tree.Project.ProjectFolder;
        tree.Project.AddScreen("Title");
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        string strayScreen = Path.Combine(folder, "Screens", "Credits.gusx");
        File.WriteAllText(strayScreen, File.ReadAllText(Path.Combine(folder, "Screens", "Title.gusx")).Replace("<Name>Title</Name>", "<Name>Credits</Name>"));
        string strayBehavior = Path.Combine(folder, "Behaviors", "Draggable.behx");
        File.WriteAllText(strayBehavior, File.ReadAllText(Path.Combine(folder, "Behaviors", "Clickable.behx")).Replace("<Name>Clickable</Name>", "<Name>Draggable</Name>"));

        tree.Dialogs.AnswerNext<ImportScreenDialog>(dialog =>
        {
            dialog.UnfilteredFiles.Select(Path.GetFileName).ShouldBe(new[] { "Credits.gusx" }, "only the file the project does not list is offered");
            dialog.SelectedFiles.Add(dialog.FilteredFiles.Single());
            return true;
        });
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Import Screen");

        tree.Dialogs.AnswerNext<ImportBehaviorDialog>(dialog =>
        {
            dialog.UnfilteredFiles.Select(Path.GetFileName).ShouldBe(new[] { "Draggable.behx" }, "only the file the project does not list is offered");
            dialog.SelectedFiles.Add(dialog.FilteredFiles.Single());
            return true;
        });
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Import Behavior");
        tree.Input.Layout();

        tree.Project.Project.Screens.Select(screen => screen.Name).ShouldBe(new[] { "Title", "Credits" }, ignoreOrder: true);
        tree.Project.Project.Behaviors.Select(behavior => behavior.Name).ShouldBe(new[] { "Clickable", "Draggable" }, ignoreOrder: true);
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "Credits", "Title" }, ignoreOrder: true);
        tree.ChildTexts(tree.RootNode("Behaviors")).ShouldBe(new[] { "Clickable", "Draggable" }, ignoreOrder: true);
        string projectText = File.ReadAllText(tree.Project.ProjectFilePath);
        projectText.ShouldContain("Credits");
        projectText.ShouldContain("Draggable");

        tree.AssertOracles();
    }

    #endregion

    #region Keeping up with changes

    [AvaloniaFact]
    [Trait("Feature", "TREE-048")]
    [Trait("Feature", "TREE-049")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void AnInstanceAddedBelowTheFold_ScrollsIntoView_AndReparentingKeepsItSelected_ThroughUndoAndRedo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave list = tree.Project.AddComponent("List");
        InstanceSave box = tree.Project.AddInstance(list, "Box", "Container");
        for (int i = 0; i < 50; i++)
        {
            tree.Project.AddInstance(list, "Item" + i, "Rectangle");
        }
        tree.Click(tree.NodeFor(list));

        tree.RightClick(tree.NodeFor(list));
        tree.PickMenu("Add object to List", "Sprite");
        InstanceSave sprite = list.Instances.Last();
        sprite.BaseType.ShouldBe("Sprite");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(sprite);
        tree.IsScrolledIntoView(tree.NodeFor(sprite)).ShouldBeTrue("the new instance's row is scrolled into view");
        ProjectFileSnapshot added = tree.SnapshotFiles();

        // Reparenting through the Variables tab rebuilds the tree; the moved node stays selected.
        tree.Grid.PickComboItem("Parent", "Box");
        list.DefaultState!.GetValue(sprite.Name + ".Parent").ShouldBe("Box");
        tree.NodeFor(sprite).Parent.ShouldBeSameAs(tree.NodeFor(box));
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(sprite);
        tree.NodeFor(sprite).IsSelected.ShouldBeTrue();
        tree.IsScrolledIntoView(tree.NodeFor(sprite)).ShouldBeTrue("the moved instance's row stays in view");

        tree.Undo();
        Instance(list, sprite.Name).ShouldNotBeNull();
        tree.NodeFor(Instance(list, sprite.Name)).Parent.ShouldBeSameAs(tree.NodeFor(list));
        tree.SnapshotFiles().ShouldMatch(added, "undoing the reparent should restore the files");
        tree.Redo();
        tree.NodeFor(Instance(list, sprite.Name)).Parent.ShouldBeSameAs(tree.NodeFor(Instance(list, "Box")));

        tree.AssertOracles();
    }

    #endregion

    #region Hotkeys

    [AvaloniaFact]
    [Trait("Feature", "KEY-010")]
    [Trait("Feature", "KEY-011")]
    public void CtrlFFocusesTheTreeSearch_AndCtrlEFocusesTheVariableFilter()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddComponent("Panel");
        tree.Click(tree.NodeFor(button));
        _ = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.F, PhysicalKey.F, RawInputModifiers.Control);
        tree.View.SearchBox.IsFocused.ShouldBeTrue("Ctrl+F puts the caret in the search box");
        tree.Input.TypeText("Pan");
        tree.Input.Layout();
        tree.View.SearchBox.Text.ShouldBe("Pan");
        tree.SearchResultTexts().ShouldBe(new[] { "Panel (Container)" });
        tree.View.ClearSearchText();
        tree.Input.Layout();

        tree.Press(Key.E, PhysicalKey.E, RawInputModifiers.Control);
        tree.Grid.Settle();
        tree.Grid.View.FilterTextBox.IsFocused.ShouldBeTrue("Ctrl+E puts the caret in the variable filter");
        tree.Grid.Input.TypeText("Width");
        tree.Grid.Settle();
        tree.Grid.ViewModel.VariableFilterText.ShouldBe("Width");
        tree.Grid.ShownMemberNames().ShouldAllBe(name => name.Contains("Width", StringComparison.OrdinalIgnoreCase));
        tree.Grid.ShownMemberNames().ShouldNotBeEmpty();
        tree.Grid.ViewModel.VariableFilterText = string.Empty;

        tree.SnapshotFiles().ShouldMatch(start, "searching and filtering change no file");
        tree.AssertOracles();
    }

    #endregion

    private static (string, FilePath) LastRequest(RecordingFileSystemRevealService reveal)
    {
        reveal.Requests.ShouldNotBeEmpty();
        string request = reveal.Requests[^1];
        int colon = request.IndexOf(": ", StringComparison.Ordinal);
        return (request[..colon], new FilePath(request[(colon + 2)..]));
    }

    private static global::Avalonia.Controls.TextBlock ProjectTitle(Gum.Avalonia.Shell.MainWindow window)
    {
        global::Avalonia.Controls.DockPanel content = ((global::Avalonia.Controls.Panel)window.Content!).Children.OfType<global::Avalonia.Controls.DockPanel>().Single();
        global::Avalonia.Controls.Grid titleRow = (global::Avalonia.Controls.Grid)content.Children.Single(child => global::Avalonia.Controls.DockPanel.GetDock(child) == global::Avalonia.Controls.Dock.Top);
        return titleRow.Children.OfType<global::Avalonia.Controls.TextBlock>().Single();
    }

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static InstanceSave Instance(ElementSave element, string name) => element.Instances.Single(instance => instance.Name == name);
}
