using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
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

            tree.Dialogs.Messages.Count.ShouldBe(1);
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
            tree.Dialogs.Messages.Count.ShouldBe(1);
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

            tree.Dialogs.Messages.Count.ShouldBe(1);
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
            tree.Dialogs.Messages.Count.ShouldBe(1);
            VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(96f);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);
}
