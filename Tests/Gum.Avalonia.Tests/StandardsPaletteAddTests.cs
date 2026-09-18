using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Gum.Avalonia.Plugins.TreeView;
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
/// The Standards palette's Ctrl+click and Ctrl+Shift+click adds, driven through the head's real
/// service graph from the palette's own actions: they reach <c>ElementTreeViewManager</c>, the
/// drag-drop path and <c>ElementCommands</c>, and the new instance lands where the gesture says.
/// The palette is taken from the head's Project tab rather than hosted in a window, since the
/// tab's content belongs to the head's main window in other tests.
/// </summary>
public class StandardsPaletteAddTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void ChipClicks_AllAddAtTheAddDestination()
    {
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        Services.GetRequiredService<Gum.Reflection.ITypeManager>().Initialize();
        StandardElementsManager.Self.Initialize();
        Services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();

        AvaloniaTabManager tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        if (tabManager.Left.All(tab => tab.Title != "Project"))
        {
            treeViewManager.Initialize();
        }
        AvaloniaPluginTab projectTab = tabManager.Left.Single(tab => tab.Title == "Project");
        AvaloniaStandardsPalette palette = ((Control)projectTab.Content).GetLogicalDescendants().OfType<AvaloniaStandardsPalette>().Single();

        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        projectManager.CreateNewProject();
        GumProjectSave project = projectManager.GumProjectSave!;
        // The tree refresh resolves folders off the project file; nothing is written there.
        project.FullFileName = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "Project.gumx");

        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        InstanceSave container = new InstanceSave { Name = "Container", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(container);
        project.Components.Add(component);
        try
        {
            treeViewManager.RefreshUi();
            // Explicit, since the shared SelectedState carries whatever an earlier test left selected
            // and an add is refused while a non-default state of the target is selected.
            selectedState.SelectedElement = component;
            selectedState.SelectedStateSave = component.DefaultState;
            selectedState.SelectedInstance = container;
            Dispatcher.UIThread.RunJobs();

            // Every add gesture lands at the add destination: the selected Container first, then the
            // Container remembered across the adds' own selection of what they created.
            palette.AddToCurrentRequested!("Text");
            InstanceSave firstText = component.Instances.Single(instance => instance.BaseType == "Text");
            component.DefaultState.GetValue($"{firstText.Name}.Parent").ShouldBe("Container");
            selectedState.SelectedInstance.ShouldBeSameAs(firstText);

            palette.AddToCurrentRequested!("Sprite");
            InstanceSave sprite = component.Instances.Single(instance => instance.BaseType == "Sprite");
            component.DefaultState.GetValue($"{sprite.Name}.Parent").ShouldBe("Container");
            selectedState.SelectedInstance.ShouldBeSameAs(sprite);
        }
        finally
        {
            selectedState.SelectedInstance = null;
            selectedState.SelectedElement = null;
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
