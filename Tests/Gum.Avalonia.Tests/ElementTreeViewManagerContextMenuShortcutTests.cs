using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Issue #4910: right-click menu items whose action already has a bound keyboard shortcut
/// (<see cref="IHotkeyManager"/>) should show that shortcut, the way "Move state up/down"
/// already does. Drives <see cref="ElementTreeViewManager"/> through the head's real service
/// graph so the shortcut text reflects the actual bound <see cref="KeyCombination"/>, not a
/// hand-typed guess.
/// </summary>
public class ElementTreeViewManagerContextMenuShortcutTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void BuildContextMenuItems_ForInstance_ShowsShortcutsMatchingHotkeyManager()
    {
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        IHotkeyManager hotkeyManager = Services.GetRequiredService<IHotkeyManager>();
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

            var items = treeViewManager.BuildContextMenuItems();

            items.Single(item => item.Text == "Go to definition").Shortcut.ShouldBe(hotkeyManager.GoToDefinition.ToString());
            items.Single(item => item.Text == "Copy").Shortcut.ShouldBe(hotkeyManager.Copy.ToString());
            items.Single(item => item.Text == "Cut").Shortcut.ShouldBe(hotkeyManager.Cut.ToString());
            items.Single(item => item.Text == "Duplicate Child").Shortcut.ShouldBe(hotkeyManager.Duplicate.ToString());
            items.Single(item => item.Text == "Delete Child").Shortcut.ShouldBe(hotkeyManager.Delete.ToString());
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
}
