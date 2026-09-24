using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head's tree mirrors whatever the tool selects. Mirroring is one-way: a node the tree cannot
/// show must not turn into a selection of its own.
/// </summary>
public class TreeSelectionSyncTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void SelectingAnInstanceTheTreeDoesNotShow_KeepsTheSelection()
    {
        // The tree holds no node for an element added since its last refresh - and the search
        // filter drops nodes the same way. Clearing the tree's own selection there used to travel
        // back out through the plugin and null the instance the caller had just selected.
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        projectManager.CreateNewProject();
        GumProjectSave project = projectManager.GumProjectSave!;
        project.FullFileName = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "Project.gumx");

        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(instance);
        project.Components.Add(component);
        try
        {
            selectedState.SelectedElement = component;
            selectedState.SelectedInstance = instance;

            selectedState.SelectedInstance.ShouldBeSameAs(instance);
            treeViewManager.SelectedNode.ShouldBeNull();
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
