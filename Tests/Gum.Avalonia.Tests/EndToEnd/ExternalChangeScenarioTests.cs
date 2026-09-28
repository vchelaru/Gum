using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios for an element file changed outside the tool while the tool has the
/// project open: with Auto Save off and unsaved edits to that element, the tool asks whether to
/// reload from disk or keep its own version (#5379).
/// </summary>
[Trait("Category", "EndToEnd")]
public class ExternalChangeScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "FILE-015")]
    [Trait("Feature", "FILE-006")]
    [Trait("Feature", "PROP-001")]
    public void KeepingUnsavedEdits_LeavesTheElementAlone_AndTheNextSaveOverwritesTheOutsideEdit()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "80");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx");
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        ComponentSave button = Component(tree, "Button");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            grid.TypeAndEnter("X", "7");
            File.WriteAllText(buttonFile, File.ReadAllText(buttonFile).Replace(">80</Value>", ">95</Value>"));

            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
            fileChanges.ReactToFileChanged(new FilePath(buttonFile));
            tree.ThrowIfCrashed();

            ShouldHaveShownOnePrompt(tree);
            tree.Dialogs.Messages[0].ShouldContain("Button");
            Component(tree, "Button").ShouldBeSameAs(button);
            VariableGridHarness.StoredValue(button, "X").ShouldBe(7f);
            VariableGridHarness.StoredValue(button, "Width").ShouldBe(80f);
            grid.FieldText("X").ShouldBe("7");

            // The kept edit is still in the element's history.
            tree.Undo();
            VariableGridHarness.StoredValue(button, "X").ShouldBeNull();
            tree.Redo();
            VariableGridHarness.StoredValue(button, "X").ShouldBe(7f);

            // Saving writes the tool's version over the outside edit.
            tree.SaveAll();
            VariableGridHarness.StoredValue(grid.ReadSaved(button), "X").ShouldBe(7f);
            VariableGridHarness.StoredValue(grid.ReadSaved(button), "Width").ShouldBe(80f);

            // Once saved, nothing is unsaved: a later outside edit reloads without asking.
            File.WriteAllText(buttonFile, File.ReadAllText(buttonFile).Replace(">80</Value>", ">96</Value>"));
            fileChanges.ReactToFileChanged(new FilePath(buttonFile));
            tree.ThrowIfCrashed();
            ShouldHaveShownOnePrompt(tree);
            VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(96f);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "FILE-015")]
    [Trait("Feature", "PROP-001")]
    public void ReloadingOverUnsavedEdits_ShowsTheOutsideEdit_AndDropsTheElementsHistory()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "80");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx");
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            grid.TypeAndEnter("X", "7");
            File.WriteAllText(buttonFile, File.ReadAllText(buttonFile).Replace(">80</Value>", ">95</Value>"));

            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            fileChanges.ReactToFileChanged(new FilePath(buttonFile));
            tree.ThrowIfCrashed();

            ShouldHaveShownOnePrompt(tree);
            ComponentSave button = Component(tree, "Button");
            VariableGridHarness.StoredValue(button, "Width").ShouldBe(95f);
            VariableGridHarness.StoredValue(button, "X").ShouldBeNull();
            tree.SelectedState.SelectedElement.ShouldBeSameAs(button);

            // The reload changed the element, so its history is dropped: undo restores nothing.
            tree.Undo();
            VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(95f);
            VariableGridHarness.StoredValue(Component(tree, "Button"), "X").ShouldBeNull();

            // The reloaded element has no unsaved edits, so the next outside edit reloads without asking.
            File.WriteAllText(buttonFile, File.ReadAllText(buttonFile).Replace(">95</Value>", ">96</Value>"));
            fileChanges.ReactToFileChanged(new FilePath(buttonFile));
            tree.ThrowIfCrashed();
            ShouldHaveShownOnePrompt(tree);
            VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(96f);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-006")]
    public void SaveAll_WritesABehaviorEditedWhileAutoSaveIsOff()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave behavior = AddBehaviorWithAutoSave(tree, "Clickable");
        string behaviorFile = Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx");
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            AddCategory(tree, "Looks");
            behavior.Categories.Select(category => category.Name).ShouldBe(new[] { "Looks" });
            File.ReadAllText(behaviorFile).ShouldNotContain("Looks");

            tree.SaveAll();

            File.ReadAllText(behaviorFile).ShouldContain("Looks");
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "FILE-015")]
    public void KeepingUnsavedBehaviorEdits_WhenItsFileChangesOutside_LeavesTheBehaviorAlone()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        BehaviorSave behavior = AddBehaviorWithAutoSave(tree, "Clickable");
        string behaviorFile = Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx");
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            AddCategory(tree, "Looks");
            File.AppendAllText(behaviorFile, Environment.NewLine);

            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
            fileChanges.ReactToFileChanged(new FilePath(behaviorFile));
            tree.ThrowIfCrashed();

            ShouldHaveShownOnePrompt(tree);
            tree.Dialogs.Messages[0].ShouldContain("Clickable");
            tree.Project.Project.Behaviors.Single().ShouldBeSameAs(behavior);
            behavior.Categories.Select(category => category.Name).ShouldBe(new[] { "Looks" });

            // Once saved, a later outside write reloads without asking. It writes the saved
            // content back, so the end-of-scenario save finds nothing to change.
            tree.SaveAll();
            File.WriteAllText(behaviorFile, File.ReadAllText(behaviorFile));
            fileChanges.ReactToFileChanged(new FilePath(behaviorFile));
            tree.ThrowIfCrashed();
            ShouldHaveShownOnePrompt(tree);
            tree.Project.Project.Behaviors.Single().ShouldNotBeSameAs(behavior);
            tree.Project.Project.Behaviors.Single().Categories.Select(category => category.Name).ShouldBe(new[] { "Looks" });
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "FILE-015")]
    [Trait("Feature", "PROP-001")]
    public void KeepingUnsavedEdits_WhenTheProjectFileChangesOutside_DoesNotReloadTheProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        GumProjectSave project = tree.Project.Project;
        ComponentSave button = Component(tree, "Button");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            grid.TypeAndEnter("X", "7");
            File.AppendAllText(tree.Project.ProjectFilePath, Environment.NewLine);

            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
            fileChanges.ReactToFileChanged(new FilePath(tree.Project.ProjectFilePath));
            tree.ThrowIfCrashed();

            ShouldHaveShownOnePrompt(tree);
            tree.Dialogs.Messages[0].ShouldContain("Harness.gumx");
            projectManager.GumProjectSave.ShouldBeSameAs(project);
            VariableGridHarness.StoredValue(button, "X").ShouldBe(7f);

            // Once everything is saved, a later outside write reloads the project without asking.
            tree.SaveAll();
            File.WriteAllText(tree.Project.ProjectFilePath, File.ReadAllText(tree.Project.ProjectFilePath));
            fileChanges.ReactToFileChanged(new FilePath(tree.Project.ProjectFilePath));
            tree.WaitUntil(() => projectManager.GumProjectSave != project, TimeSpan.FromSeconds(30), "the project to reload");
            ShouldHaveShownOnePrompt(tree);
            ComponentSave reloadedButton = projectManager.GumProjectSave!.Components.Single(component => component.Name == "Button");
            VariableGridHarness.StoredValue(reloadedButton, "X").ShouldBe(7f);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    /// <summary>Exactly one message was shown; on failure, names every message and the file watcher's queue.</summary>
    private static void ShouldHaveShownOnePrompt(ProjectTreeHarness tree)
    {
        IFileWatchManager fileWatch = TestAppBuilder.Services.GetRequiredService<IFileWatchManager>();
        tree.Dialogs.Messages.Count.ShouldBe(1,
            $"Messages: [{string.Join(" | ", tree.Dialogs.Messages)}]. File watch enabled {fileWatch.Enabled}, " +
            $"waiting [{string.Join(", ", fileWatch.ChangedFilesWaitingForFlush)}].");
    }

    /// <summary>Adds a behavior from the Behaviors menu, which saves it while Auto Save is on.</summary>
    private static BehaviorSave AddBehaviorWithAutoSave(ProjectTreeHarness tree, string name)
    {
        tree.Dialogs.AnswerNextUserString(name);
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        return tree.Project.Project.Behaviors.Single(behavior => behavior.Name == name);
    }

    /// <summary>Adds a category to the selected behavior from the States tab.</summary>
    private static void AddCategory(ProjectTreeHarness tree, string name)
    {
        tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = name; return true; });
        tree.States.ClickNewCategory();
    }

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);
}
