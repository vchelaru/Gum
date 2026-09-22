using Avalonia.Headless.XUnit;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
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
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        Services.GetRequiredService<Gum.Reflection.ITypeManager>().Initialize();
        StandardElementsManager.Self.Initialize();
        Services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();

        ITabManager tabManager = Services.GetRequiredService<ITabManager>();
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

        ElementTreeViewManager treeViewManager = ActivatorUtilities.CreateInstance<ElementTreeViewManager>(Services);
        treeViewManager.Initialize();
        AvaloniaPluginTab projectTab = ((AvaloniaTabManager)tabManager).Left.Last(tab => tab.Title == "Project");
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
            ObjectFinder.Self.GumProjectSave = null;
            tabManager.RemoveTab(projectTab);
        }
    }
}
