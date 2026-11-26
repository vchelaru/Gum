using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A flush of many changed files defers the project initialize, tree refresh and canvas rebuild to the end of the
/// batch (#8445). These run the real tool services to check the deferred work still leaves the project, tree and
/// selection right.
/// </summary>
[Trait("Category", "EndToEnd")]
public class FileWatchBatchReloadScenarioTests
{
    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    /// <summary>Button and Label, and a selected Panel holding an instance of each, saved to disk.</summary>
    private static ProjectTreeHarness Build(out string[] files)
    {
        ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Project.AddComponent("Label");
        ComponentSave panel = tree.Project.AddComponent("Panel");
        tree.Project.AddInstance(panel, "ButtonInstance", "Button");
        tree.Project.AddInstance(panel, "LabelInstance", "Label");
        tree.SaveAll();
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        files = new[] { "Button", "Label", "Panel" }
            .Select(name => Path.Combine(tree.Project.ProjectFolder, "Components", name + ".gucx"))
            .ToArray();
        return tree;
    }

    [AvaloniaFact]
    public void Batch_ShouldReloadTheChangedElementAndKeepTheSelection()
    {
        using ProjectTreeHarness tree = Build(out string[] files);
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        ComponentSave oldButton = Component(tree, "Button");
        ComponentSave oldPanel = Component(tree, "Panel");

        // Button gets a real change; the rest are rewritten with the same bytes, like a CLI edit.
        string buttonText = File.ReadAllText(files[0]);
        string newLine = Environment.NewLine;
        string changed = buttonText.Replace(
            "  </State>",
            "    <Variable Type=\"float\" Name=\"Width\" SetsValue=\"true\">" + newLine +
            "      <Value xsi:type=\"xsd:float\">123</Value>" + newLine +
            "    </Variable>" + newLine +
            "  </State>",
            StringComparison.Ordinal);
        changed.ShouldNotBe(buttonText, "the Button file has no Default state to edit");
        File.WriteAllText(files[0], changed);
        foreach (string file in files.Skip(1))
        {
            File.WriteAllText(file, File.ReadAllText(file));
        }

        using (fileChanges.DeferDependentUpdates())
        {
            foreach (string file in files)
            {
                fileChanges.ReactToFileChanged(new FilePath(file));
            }
        }
        tree.ThrowIfCrashed();

        ComponentSave newButton = Component(tree, "Button");
        newButton.ShouldNotBeSameAs(oldButton);
        VariableGridHarness.StoredValue(newButton, "Width").ShouldBe(123f);
        ComponentSave newPanel = Component(tree, "Panel");
        newPanel.ShouldNotBeSameAs(oldPanel);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(newPanel);
        tree.NodeFor(newButton).ShouldNotBeNull();
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void Batch_WithTheProjectFile_ShouldReloadTheProjectAndPickUpTheElementEdit()
    {
        using ProjectTreeHarness tree = Build(out string[] files);
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        GumProjectSave project = projectManager.GumProjectSave!;

        string buttonText = File.ReadAllText(files[0]);
        File.WriteAllText(files[0], buttonText.Replace(
            "<Name>Default</Name>",
            "<Name>Default</Name><Variable Type=\"float\" Name=\"Width\" SetsValue=\"true\"><Value xsi:type=\"xsd:float\">77</Value></Variable>",
            StringComparison.Ordinal));
        File.WriteAllText(tree.Project.ProjectFilePath, File.ReadAllText(tree.Project.ProjectFilePath));

        using (fileChanges.DeferDependentUpdates())
        {
            // Flush reacts to the project file first.
            fileChanges.ReactToFileChanged(new FilePath(tree.Project.ProjectFilePath));
            foreach (string file in files)
            {
                fileChanges.ReactToFileChanged(new FilePath(file));
            }
        }
        tree.WaitUntil(() => projectManager.GumProjectSave != project, TimeSpan.FromSeconds(30), "the project to reload");
        tree.ThrowIfCrashed();

        tree.Dialogs.Messages.ShouldBeEmpty();
        GumProjectSave reloaded = projectManager.GumProjectSave!;
        ComponentSave button = reloaded.Components.Single(component => component.Name == "Button");
        VariableGridHarness.StoredValue(button, "Width").ShouldBe(77f);
        tree.WaitUntil(() => tree.SelectedState.SelectedElement?.Name == "Panel", TimeSpan.FromSeconds(10), "Panel reselected");
        reloaded.Components.ShouldContain(tree.SelectedState.SelectedElement as ComponentSave);
    }
}
