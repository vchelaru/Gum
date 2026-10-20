using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Startup;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The Variables tab for an instance of a shipped Forms control. Loads the real Standard template,
/// so what the tab shows is what a user who just ran Add Forms sees.
/// </summary>
public class FormsTemplateInstanceVariablesScenarioTests
{
    [AvaloniaFact]
    public void ScrollBarInstance_ShowsOrientation_NotTheOrientationCategoryState()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollBar = AddStandardInstance(grid, "ScrollBarInstance", "Controls/ScrollBar");

        grid.Select(scrollBar);

        grid.ShownMemberNames().ShouldContain("ScrollBarInstance.Orientation");
        grid.ShownMemberNames().ShouldNotContain("ScrollBarInstance.OrientationCategoryState");
        grid.ShownMemberNames().ShouldNotContain("ScrollBarInstance.ScrollBarCategoryState");
    }

    [AvaloniaFact]
    public void ScrollBarInstance_PickingHorizontalOrientation_DrivesTheHiddenState_AndVerticalClearsIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollBar = AddStandardInstance(grid, "ScrollBarInstance", "Controls/ScrollBar");
        ElementSave owner = grid.SelectedState.SelectedElement!;
        grid.Select(scrollBar);

        grid.PickComboItem("Orientation", "Horizontal");

        VariableGridHarness.StoredValue(owner, "ScrollBarInstance.OrientationCategoryState").ShouldBe("Horizontal");

        grid.PickComboItem("Orientation", "Vertical");

        VariableGridHarness.StoredValue(owner, "ScrollBarInstance.OrientationCategoryState").ShouldBeNull();
    }

    [AvaloniaFact]
    public void ScrollBarInstance_HorizontalOrientation_SurvivesSaveAndReload()
    {
        using EnumFixups fixups = new EnumFixups();
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollBar = AddStandardInstance(grid, "ScrollBarInstance", "Controls/ScrollBar");
        grid.Select(scrollBar);
        grid.PickComboItem("Orientation", "Horizontal");

        grid.Project.SaveAndReload();
        ElementSave screen = grid.Project.Project.Screens.Single(item => item.Name == "TemplateScreen");
        grid.Select(screen);
        grid.Select(screen.Instances.Single(item => item.Name == "ScrollBarInstance"));

        grid.ComboText("Orientation").ShouldBe("Horizontal");
        VariableGridHarness.StoredValue(screen, "ScrollBarInstance.OrientationCategoryState").ShouldBe("Horizontal");
    }

    [AvaloniaFact]
    public void ScrollBarInstance_AuthoredWithOnlyTheHorizontalState_ReloadsAsHorizontalOrientation()
    {
        using EnumFixups fixups = new EnumFixups();
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollBar = AddStandardInstance(grid, "ScrollBarInstance", "Controls/ScrollBar");
        grid.SelectedState.SelectedElement!.DefaultState!.SetValue(
            "ScrollBarInstance.OrientationCategoryState", "Horizontal", "OrientationCategory");

        grid.Project.SaveAndReload();
        ElementSave screen = grid.Project.Project.Screens.Single(item => item.Name == "TemplateScreen");
        grid.Select(screen);
        grid.Select(screen.Instances.Single(item => item.Name == "ScrollBarInstance"));

        grid.ComboText("Orientation").ShouldBe("Horizontal");
        VariableGridHarness.StoredValue(screen, "ScrollBarInstance.OrientationCategoryState").ShouldBe("Horizontal");
    }

    [AvaloniaFact]
    public void ScrollViewerComponent_KeepsItsHorizontalScrollBarHorizontal_WhenSelected()
    {
        using EnumFixups fixups = new EnumFixups();
        using VariableGridHarness grid = new VariableGridHarness();
        AddStandardInstance(grid, "ScrollViewerInstance", "Controls/ScrollViewer");
        ComponentSave scrollViewer = grid.Project.Project.Components.Single(item => item.Name == "Controls/ScrollViewer");

        grid.Select(scrollViewer);
        grid.Select(scrollViewer.Instances.Single(item => item.Name == "HorizontalScrollBarInstance"));

        VariableGridHarness.StoredValue(scrollViewer, "HorizontalScrollBarInstance.OrientationCategoryState").ShouldBe("Horizontal");
        VariableGridHarness.StoredValue(scrollViewer, "HorizontalScrollBarInstance.Orientation").ShouldBe(Gum.Forms.Controls.Orientation.Horizontal);
    }

    [AvaloniaFact]
    public void ScrollViewerInstance_AlreadyAuthoringTheCategoryState_KeepsShowingIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollViewer = AddStandardInstance(grid, "ScrollViewerInstance", "Controls/ScrollViewer");
        ElementSave owner = grid.SelectedState.SelectedElement!;
        owner.DefaultState!.SetValue("ScrollViewerInstance.ScrollViewerCategoryState", "Focused", "ScrollViewerCategory");

        grid.Select(owner);
        grid.Select(scrollViewer);

        grid.ShownMemberNames().ShouldContain("ScrollViewerInstance.ScrollViewerCategoryState");
    }

    [AvaloniaFact]
    public void ScrollViewerInstance_DoesNotShowTheScrollViewerCategoryState()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave scrollViewer = AddStandardInstance(grid, "ScrollViewerInstance", "Controls/ScrollViewer");

        grid.Select(scrollViewer);

        grid.ShownMemberNames().ShouldNotContain("ScrollViewerInstance.ScrollViewerCategoryState");
    }

    /// <summary>
    /// The enum fix-up the tool wires at startup, which promotes a saved int back to the enum on
    /// load. The test app does not run the startup sequence.
    /// </summary>
    private sealed class EnumFixups : IDisposable
    {
        private readonly Func<VariableSave, bool>? _original = VariableSaveExtensionMethods.CustomFixEnumerations;

        public EnumFixups() => GumStartupSequence.WireEnumFixups();

        public void Dispose() => VariableSaveExtensionMethods.CustomFixEnumerations = _original;
    }

    /// <summary>Loads the Standard template into the harness's project and adds an instance of <paramref name="type"/> to a new screen.</summary>
    internal static InstanceSave AddStandardInstance(VariableGridHarness grid, string instanceName, string type)
    {
        LoadStandardTemplate(grid.Project);
        ScreenSave screen = grid.Project.AddScreen("TemplateScreen");
        return grid.Project.AddInstance(screen, instanceName, type);
    }

    /// <summary>
    /// Copies the Standard Forms template (and the behaviors it points at) into the fixture's folder
    /// and opens it through the tool's own load path.
    /// </summary>
    internal static void LoadStandardTemplate(ToolProjectFixture fixture)
    {
        string templates = FindTemplatesDirectory();
        string target = Path.Combine(fixture.ProjectFolder, "Templates");
        CopyDirectory(Path.Combine(templates, "FormsTemplate"), Path.Combine(target, "FormsTemplate"));
        CopyDirectory(Path.Combine(templates, "FormsBehaviors"), Path.Combine(target, "FormsBehaviors"));

        string projectFile = Path.Combine(target, "FormsTemplate", "GumProject.gumx");
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        Task load = projectManager.LoadProjectAsync(new FilePath(projectFile));
        // [AvaloniaFact] tests stay synchronous; the load posts work to the UI thread it waits on.
        while (!load.IsCompleted)
        {
            Thread.Sleep(10);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }
        load.GetAwaiter().GetResult();
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        fixture.FollowProjectFile(projectFile);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    /// <summary>
    /// The repo's templates folder. <c>GUM_TEMPLATES_DIR</c> points it at another copy, such as a
    /// checkout of <c>origin/main</c>, for the "before" half of a screenshot.
    /// </summary>
    internal static string FindTemplatesDirectory()
    {
        string? overridePath = Environment.GetEnvironmentVariable("GUM_TEMPLATES_DIR");
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate the templates directory from " + AppContext.BaseDirectory);
    }
}
