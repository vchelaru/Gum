using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Checks any end-to-end scenario can run at its end, whatever it drove. Each one looks at the
/// project the way a later session or a build step would, rather than at the objects the scenario
/// touched, so it catches damage the scenario's own assertions do not look for.
/// </summary>
internal static class ProjectOracles
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    /// <summary>
    /// What the tool auto-saved matches a full Save All; the saved project passes the same check
    /// <c>gumcli check</c> runs; reloading it in the tool reports no load errors; and saving the
    /// reloaded project changes no file. Leaves the reloaded project loaded.
    /// </summary>
    /// <param name="checkSaved">More checks of the saved project, run after the save and before the reload.</param>
    public static void AssertSaveReloadAndCheckClean(ToolProjectFixture fixture, Action? checkSaved = null)
    {
        IFileCommands fileCommands = Services.GetRequiredService<IFileCommands>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        string projectPath = fixture.ProjectFilePath;

        ProjectFileSnapshot autoSaved = ProjectFileSnapshot.Take(fixture.ProjectFolder);
        fileCommands.ForceSaveProject(forceSaveContainedElements: true);
        ProjectFileSnapshot saved = ProjectFileSnapshot.Take(fixture.ProjectFolder);
        saved.ShouldMatch(autoSaved, "Save All wrote changes the tool had not auto-saved");

        AssertCheckClean(projectPath);
        checkSaved?.Invoke();

        fixture.SaveAndReload();
        projectManager.HaveErrorsOccurredLoadingProject.ShouldBeFalse("reloading the saved project reported errors");
        fileCommands.ForceSaveProject(forceSaveContainedElements: true);
        ProjectFileSnapshot.Take(fixture.ProjectFolder).ShouldMatch(saved, "saving the reloaded project changed it");
    }

    /// <summary>
    /// Loads the project at <paramref name="projectPath"/> from disk and runs the checks
    /// <c>gumcli check</c> runs (load errors plus <see cref="HeadlessErrorChecker"/>); throws on any GUM0009 warning or any
    /// error. The tool's own project stays loaded.
    /// </summary>
    public static void AssertCheckClean(string projectPath)
    {
        // The checker points the global finder at the project it checks; the tool's must come back.
        GumProjectSave? toolProject = ObjectFinder.Self.GumProjectSave;
        List<ErrorResult> errors = new List<ErrorResult>();
        try
        {
            ProjectLoadResult loadResult = new ProjectLoader().Load(projectPath);
            loadResult.Success.ShouldBeTrue($"gumcli check could not load the project: {loadResult.ErrorMessage}");
            HeadlessErrorChecker checker = new HeadlessErrorChecker(
                new DefaultTypeResolver(),
                new IAdditionalErrorSource[] { new AnimationKeyframeErrorSource(new FileElementAnimationsProvider()) });
            errors.AddRange(loadResult.LoadErrors);
            errors.AddRange(checker.GetAllErrors(loadResult.Project!));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = toolProject;
        }
        // GUM0009 (a reference to something the project lacks) is only a warning so it doesn't
        // block codegen, but no scenario should leave one behind.
        errors.Where(error => error.Severity == ErrorSeverity.Error || error.Code == "GUM0009")
            .Select(error => $"{error.ElementName}: {error.Message}")
            .ShouldBeEmpty("gumcli check reports errors in the saved project");
    }

    /// <summary>
    /// The Project tree shows exactly the saved project's folders, screens and components, and under
    /// each element its instances with their saved parents, in their saved order.
    /// </summary>
    public static void AssertTreeMatchesSavedProject(ElementTreeViewManager tree, string projectPath)
    {
        Dispatcher.UIThread.RunJobs();
        GumProjectSave saved = GumProjectSave.Load(projectPath, out GumLoadResult loadResult)
            ?? throw new InvalidOperationException($"The saved project did not load: {loadResult.ErrorMessage}");
        string projectFolder = Path.GetDirectoryName(projectPath)!;

        List<string> expected = new List<string>();
        foreach ((string root, IEnumerable<ElementSave> elements) in new (string, IEnumerable<ElementSave>)[] { ("Screens", saved.Screens), ("Components", saved.Components) })
        {
            string rootFolder = Path.Combine(projectFolder, root);
            if (Directory.Exists(rootFolder))
            {
                expected.AddRange(Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories)
                    .Select(folder => $"{root}/{Path.GetRelativePath(rootFolder, folder).Replace('\\', '/')}/"));
            }
            foreach (ElementSave element in elements)
            {
                string elementPath = $"{root}/{element.Name}";
                expected.Add(elementPath);
                DescribeSavedInstances(element, parentName: null, elementPath, expected);
            }
        }

        List<string> shown = new List<string>();
        foreach (GumTreeNode rootNode in tree.View.Nodes.Where(node => node.Text is "Screens" or "Components"))
        {
            DescribeTreeNodes(rootNode, rootNode.Text + "/", shown);
        }

        expected.Sort(StringComparer.Ordinal);
        shown.Sort(StringComparer.Ordinal);
        shown.ShouldBe(expected, "the Project tree does not show what was saved");
    }

    private static void DescribeSavedInstances(ElementSave element, string? parentName, string path, List<string> lines)
    {
        List<InstanceSave> children = element.Instances.Where(instance => SavedParentName(element, instance) == parentName).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            string line = $"{path} > [{i}] {children[i].Name}";
            lines.Add(line);
            DescribeSavedInstances(element, children[i].Name, line, lines);
        }
    }

    // The tree nests an instance under the instance its Parent names (up to any ".slot"), and at
    // the element's root when the Parent names no instance of the element.
    private static string? SavedParentName(ElementSave element, InstanceSave instance)
    {
        if (element.DefaultState?.GetValue(instance.Name + ".Parent") is not string parent || parent.Length == 0)
        {
            return null;
        }
        int slot = parent.IndexOf('.');
        string parentInstance = slot >= 0 ? parent.Substring(0, slot) : parent;
        return element.Instances.Any(candidate => candidate.Name == parentInstance) ? parentInstance : null;
    }

    private static void DescribeTreeNodes(GumTreeNode node, string path, List<string> lines)
    {
        foreach (GumTreeNode child in node.Nodes)
        {
            switch (child.Tag)
            {
                case null:
                    lines.Add($"{path}{child.Text}/");
                    DescribeTreeNodes(child, $"{path}{child.Text}/", lines);
                    break;
                case ElementSave:
                    lines.Add($"{path}{child.Text}");
                    DescribeInstanceNodes(child, $"{path}{child.Text}", lines);
                    break;
                default:
                    lines.Add($"{path}?{child.Text} ({child.Tag.GetType().Name})");
                    break;
            }
        }
    }

    private static void DescribeInstanceNodes(GumTreeNode node, string path, List<string> lines)
    {
        for (int i = 0; i < node.Nodes.Count; i++)
        {
            GumTreeNode child = node.Nodes[i];
            string line = $"{path} > [{i}] {child.Text}";
            lines.Add(child.Tag is InstanceSave ? line : $"{line} ({child.Tag?.GetType().Name ?? "no tag"})");
            DescribeInstanceNodes(child, line, lines);
        }
    }
}
