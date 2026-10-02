using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Gum.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The element tree's right-click menu shows each bound <see cref="IHotkeyManager"/> shortcut
/// (#4910) with the platform's command modifier: Cmd on macOS, which Avalonia renders as ⌘ (#5569).
/// </summary>
public class ElementTreeViewManagerContextMenuShortcutTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaTheory]
    [InlineData(KeyModifiers.Meta)]
    [InlineData(KeyModifiers.Control)]
    public void ContextMenu_ForInstance_ShowsEachShortcutWithThePlatformCommandModifier(KeyModifiers command)
    {
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        projectManager.CreateNewProject();
        GumProjectSave project = projectManager.GumProjectSave!;
        project.FullFileName = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "Project.gumx");

        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(instance);
        project.Components.Add(component);

        // The head's own tree, the one its plugin syncs selection into. A second manager would
        // fight this one over ISelectedState: the plugin selects into the tree it owns, and a
        // node it cannot find there clears the selection the test just made.
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        try
        {
            treeViewManager.RefreshUi();
            GumTreeNode componentNode = treeViewManager.GetTreeNodeFor(component)!;
            GumTreeNode instanceNode = treeViewManager.GetTreeNodeFor(instance, componentNode)!;
            treeViewManager.SelectedNode = instanceNode;
            selectedState.SelectedInstance = instance;

            List<ContextMenuItemViewModel> items = treeViewManager.BuildContextMenuItems().ToList();
            ContextMenu menu = new ContextMenu();
            AvaloniaContextMenus.Populate(menu, items, AvaloniaContextMenus.DefaultIconSize, command);

            Gesture(menu, "Go to definition").ShouldBe(new KeyGesture(Key.F12));
            Gesture(menu, "Copy").ShouldBe(new KeyGesture(Key.C, command));
            Gesture(menu, "Cut").ShouldBe(new KeyGesture(Key.X, command));
            Gesture(menu, "Duplicate Child").ShouldBe(new KeyGesture(Key.D, command));
            // Delete's primary key is Backspace on macOS (#5620), whatever modifier the test simulates.
            Key deleteKey = OperatingSystem.IsMacOS() ? Key.Back : Key.Delete;
            Gesture(menu, "Delete Child").ShouldBe(new KeyGesture(deleteKey));
        }
        finally
        {
            selectedState.SelectedInstance = null;
            selectedState.SelectedElement = null;
            treeViewManager.SelectedNode = null;
            ObjectFinder.Self.GumProjectSave = null;
            // The project manager keeps this project, and IProjectState reads the folder off it, so
            // a later test would walk a directory that was never created.
            project.FullFileName = null!;
        }
    }

    [AvaloniaFact]
    public void ToMenuItem_ShowsSubmenuShortcutsWithThePlatformCommandModifier()
    {
        // Every view-model menu (States tree, Animations, canvas) renders through this, submenus included.
        ContextMenuItemViewModel parent = new ContextMenuItemViewModel { Text = "Edit" };
        parent.Children.Add(new ContextMenuItemViewModel { Text = "Paste", Shortcut = KeyCombination.Ctrl(Gum.Input.GumKey.V) });
        parent.Children.Add(new ContextMenuItemViewModel { Text = "Move Up", Shortcut = KeyCombination.Alt(Gum.Input.GumKey.Up) });

        MenuItem menuItem = (MenuItem)AvaloniaContextMenus.ToMenuItem(parent, AvaloniaContextMenus.DefaultIconSize, KeyModifiers.Meta);

        MenuItem[] children = menuItem.Items.OfType<MenuItem>().ToArray();
        children[0].InputGesture.ShouldBe(new KeyGesture(Key.V, KeyModifiers.Meta));
        children[1].InputGesture.ShouldBe(new KeyGesture(Key.Up, KeyModifiers.Alt));
    }

    private static KeyGesture? Gesture(ContextMenu menu, string header) =>
        menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == header).InputGesture;
}
