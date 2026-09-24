using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// One error check per edit, through the head's own plugins (issue #4950). The Errors tab and the
/// tree's "!" both want the result, so the tab checks and the tree follows
/// <see cref="IErrorChecker.ErrorsChecked"/>; a check walks the element's file references and reads
/// the disk, so a second one is real work wasted.
/// </summary>
public class ErrorCheckOncePerEditTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaTheory]
    [InlineData("VariableSet", 1)]
    [InlineData("VariableSetWhileDragging", 0)]
    [InlineData("ElementReloaded", 1)]
    [InlineData("InstanceAdd", 1)]
    [InlineData("InstanceDelete", 1)]
    [InlineData("VariableRemovedFromCategory", 1)]
    [InlineData("BehaviorReferencesChanged", 1)]
    public void ANotification_ChecksTheSelectedElement_TheExpectedNumberOfTimes(string notification, int expectedChecks)
    {
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        IErrorChecker errorChecker = Services.GetRequiredService<IErrorChecker>();
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        projectManager.CreateNewProject();
        GumProjectSave project = projectManager.GumProjectSave!;
        string projectFolder = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectFolder);
        project.FullFileName = Path.Combine(projectFolder, "Project.gumx");

        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(instance);
        project.Screens.Add(screen);
        StateSaveCategory category = new StateSaveCategory { Name = "MyCategory" };
        screen.Categories.Add(category);

        List<ElementSave> checkedElements = new();
        void Record(ElementSave element, ErrorViewModel[] errors) => checkedElements.Add(element);
        errorChecker.ErrorsChecked += Record;
        try
        {
            // The tree holds the screen's node, so its "!" indicator is on the real path too.
            treeViewManager.RefreshUi();
            selectedState.SelectedElement = screen;
            // Selecting is itself an edit the Errors tab checks for; the count starts after it.
            checkedElements.Clear();

            switch (notification)
            {
                case "VariableSet":
                    pluginManager.VariableSet(screen, null, "X", 0f, isFullCommit: true);
                    break;
                case "VariableSetWhileDragging":
                    pluginManager.VariableSet(screen, null, "X", 0f, isFullCommit: false);
                    break;
                case "ElementReloaded":
                    pluginManager.ElementReloaded(screen);
                    break;
                case "InstanceAdd":
                    pluginManager.InstanceAdd(screen, instance);
                    break;
                case "InstanceDelete":
                    pluginManager.InstanceDelete(screen, instance);
                    break;
                case "VariableRemovedFromCategory":
                    pluginManager.VariableRemovedFromCategory("X", category);
                    break;
                case "BehaviorReferencesChanged":
                    pluginManager.BehaviorReferencesChanged(screen);
                    break;
            }

            checkedElements.ShouldAllBe(element => element == screen);
            checkedElements.Count.ShouldBe(expectedChecks);
        }
        finally
        {
            errorChecker.ErrorsChecked -= Record;
            selectedState.SelectedElement = null;
            treeViewManager.SelectedNode = null;
            ObjectFinder.Self.GumProjectSave = null;
            // The project manager keeps this project, and IProjectState reads the folder off it, so
            // a later test would walk a directory this one just deleted.
            project.FullFileName = null!;
            Directory.Delete(projectFolder, recursive: true);
        }
    }
}
