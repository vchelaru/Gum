using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The shared oracles pass on a project nobody edited, and each one fails on the damage it exists
/// to catch, so a green scenario means the checks ran rather than that they cannot fail.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ProjectOracleTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void OpenedProject_PassesEveryOracle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ATabAScenarioLeftShowing_IsHiddenAgainWhenTheHarnessIsDisposed()
    {
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        AvaloniaPluginTab animations = tabs.AllTabs.Single(tab => tab.Title == "Animations");
        animations.Hide();

        using (ProjectTreeHarness tree = new ProjectTreeHarness())
        {
            ComponentSave button = tree.Project.AddComponent("Button");
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonAnimations.ganx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<ElementAnimationsSave>\r\n  <Animations />\r\n</ElementAnimationsSave>");
            tree.Click(tree.RootNode("Components"));
            tree.Click(tree.NodeFor(button));
            animations.IsVisible.ShouldBeTrue("selecting an element with an animation file shows the tab");
        }

        animations.IsVisible.ShouldBeFalse("the tab outlives the test, and showing would break the next one's View Animations");
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-001")]
    public void NewProject_ReloadResavesUnchanged()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumProjectOracles");
        Services.GetRequiredService<IFileCommands>().ForceSaveProject(forceSaveContainedElements: true);
        ProjectFileSnapshot created = ProjectFileSnapshot.Take(fixture.ProjectFolder);

        fixture.SaveAndReload();

        ProjectFileSnapshot.Take(fixture.ProjectFolder).ShouldMatch(created, "saving a reopened new project changed it");
    }

    [AvaloniaFact]
    public void AnEditTheToolDidNotSave_FailsTheSaveOracle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");

        button.DefaultState!.SetValue("Width", 321f, "float");

        Should.Throw<ShouldAssertException>(tree.AssertOracles).Message.ShouldContain("had not auto-saved");
    }

    [AvaloniaFact]
    public void AFileThatChanged_FailsTheFileComparison()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ProjectFileSnapshot before = tree.SnapshotFiles();

        File.AppendAllText(tree.Project.ProjectFilePath, "<!-- edited -->");

        Should.Throw<ShouldAssertException>(() => tree.SnapshotFiles().ShouldMatch(before, "nothing was edited"))
            .Message.ShouldContain("changed: " + Path.GetFileName(tree.Project.ProjectFilePath));
    }

    [AvaloniaFact]
    public void AnInstanceOfAMissingType_FailsTheCheckOracle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Ghost", BaseType = "NoSuchComponent", ParentContainer = button });
        tree.SaveAll();

        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath))
            .Message.ShouldContain("Button");
        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(tree.Project.Project);
    }

    [AvaloniaFact]
    public void AReferenceToAMissingComponent_FailsTheCheckOracle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        VariableListSave<string> references = new VariableListSave<string> { Name = "VariableReferences", Type = "string" };
        references.Value.Add("Width = Components/NoSuchStyles.Width");
        button.DefaultState!.VariableLists.Add(references);
        tree.SaveAll();

        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath))
            .Message.ShouldContain("Components/NoSuchStyles");
        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(tree.Project.Project);
    }

    [AvaloniaFact]
    public void ATreeMissingANode_FailsTheTreeOracle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");

        tree.RootNode("Components").Nodes.Remove(tree.NodeFor(button));

        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertTreeMatchesSavedProject(tree.TreeManager, tree.Project.ProjectFilePath))
            .Message.ShouldContain("Components/Button");
        tree.TreeManager.RefreshUi();
    }

    [AvaloniaFact]
    public void AnOutputErrorOrAUiThreadException_FailsTheExceptionWatch()
    {
        using ToolExceptionWatch outputWatch = new ToolExceptionWatch();
        Services.GetRequiredService<IOutputManager>().AddError("Something broke");
        Should.Throw<ShouldAssertException>(outputWatch.AssertClean).Message.ShouldContain("Something broke");

        using ToolExceptionWatch crashWatch = new ToolExceptionWatch();
        Dispatcher.UIThread.Post(() => throw new InvalidOperationException("A posted job threw"));
        Dispatcher.UIThread.RunJobs();
        Should.Throw<ShouldAssertException>(crashWatch.ThrowIfCrashed).Message.ShouldContain("A posted job threw");
    }
}
