using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.DataTypes.Variables;
using System.Text.RegularExpressions;
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

    [AvaloniaFact]
    [Trait("Feature", "FILE-002")]
    [Trait("Feature", "FILE-006")]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "PROP-001")]
    [Trait("Feature", "CANV-024")]
    public void OpeningAProjectWithoutCanvasSizes_FillsThemWithoutAnUnsavedEdit_AndTheNextSaveWritesThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        IUnsavedChangesTracker unsavedChanges = TestAppBuilder.Services.GetRequiredService<IUnsavedChangesTracker>();
        string projectFile = tree.Project.ProjectFilePath;
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            // A project saved without canvas sizes, as older projects are.
            tree.Project.Project.CustomCanvasSizes = null;
            tree.Project.SaveAndReload();
            File.ReadAllText(projectFile).ShouldNotContain("CustomCanvasSize");

            GumProjectSave opened = tree.Project.Project;
            opened.CustomCanvasSizes.ShouldNotBeNull().Select(size => size.FriendlyName).ShouldContain("1080p");
            unsavedChanges.HasAnyUnsavedChanges(opened).ShouldBeFalse();
            File.ReadAllText(projectFile).ShouldNotContain("CustomCanvasSize");

            // Nothing is unsaved, so an outside change to the project file reloads it without asking.
            File.AppendAllText(projectFile, Environment.NewLine);
            fileChanges.ReactToFileChanged(new FilePath(projectFile));
            tree.WaitUntil(() => projectManager.GumProjectSave != opened, TimeSpan.FromSeconds(30), "the project to reload");
            tree.ThrowIfCrashed();
            tree.Dialogs.Messages.ShouldBeEmpty();
            tree.Project.FollowProjectFile(projectFile);

            // A real edit and save writes the filled sizes along with the edit.
            string buttonBeforeEdit = File.ReadAllText(buttonFile);
            tree.Click(tree.NodeFor(Component(tree, "Button")));
            tree.Grid.TypeAndEnter("X", "7");
            tree.SaveAll();
            File.ReadAllText(projectFile).ShouldContain("1080p");
            File.ReadAllText(buttonFile).ShouldNotBe(buttonBeforeEdit);

            // Undoing the edit and saving restores the element file byte for byte.
            tree.Undo();
            tree.SaveAll();
            File.ReadAllText(buttonFile).ShouldBe(buttonBeforeEdit);
            File.ReadAllText(projectFile).ShouldContain("1080p");
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "VARREF-001")]
    public void ReloadingAnElementOutsideTheTool_UpdatesAndSavesTheComponentsThatReadIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness("Harness.gumj");
        ComponentSave styles = tree.Project.AddComponent("Styles");
        tree.Project.AddInstance(styles, "Primary", "Rectangle");
        styles.DefaultState.SetValue("Primary.FillRed", 21, "int");
        ComponentSave button = tree.Project.AddComponent("ButtonStandard");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        button.DefaultState.SetValue("Background.Red", 21, "int");
        button.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences",
            Type = "string",
            Value = new List<string> { "Red = Components/Styles.Primary.FillRed" },
        });
        tree.Click(tree.NodeFor(button));
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        IFileWatchIgnoreList ignoreList = TestAppBuilder.Services.GetRequiredService<IFileWatchIgnoreList>();
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        string stylesFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Styles.gucj");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonStandard.gucj");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = true;
            tree.SaveAll();
            File.WriteAllText(stylesFile, Regex.Replace(File.ReadAllText(stylesFile),
                @"(""Name"": ""Primary\.FillRed"",\s*""Type"": ""int"",\s*""SetsValue"": true,\s*""ValueAsInt"": )\d+", "${1}200"));
            DateTime stylesIgnoredUntil = ignoreList.TimedChangesToIgnore[new FilePath(stylesFile)];
            DateTime buttonIgnoredUntil = ignoreList.TimedChangesToIgnore[new FilePath(buttonFile)];
            Thread.Sleep(20);

            fileChanges.ReactToFileChanged(new FilePath(stylesFile));
            tree.ThrowIfCrashed();

            // The reader follows the new value in memory, in the Variables tab and on disk.
            ComponentSave reloadedButton = Component(tree, "ButtonStandard");
            VariableGridHarness.StoredValue(reloadedButton, "Background.Red").ShouldBe(200);
            VariableGridHarness.StoredValue(
                ElementReference.DeserializeElement<ComponentSave>(buttonFile, GumProjectSave.NativeVersion), "Background.Red").ShouldBe(200);
            // The reader's file is ignored again (the tool wrote it); the reloaded file is not rewritten.
            ignoreList.TimedChangesToIgnore[new FilePath(buttonFile)].ShouldBeGreaterThan(buttonIgnoredUntil);
            ignoreList.TimedChangesToIgnore[new FilePath(stylesFile)].ShouldBe(stylesIgnoredUntil);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    // A reader keeps its undo history across an outside edit of the element it reads. Undoing an
    // in-tool edit made before the reload must not bring back the value the reload replaced.
    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "VARREF-001")]
    public void UndoingAnEditMadeBeforeAnOutsideReload_KeepsTheValueTheReloadGaveTheReaders(bool viaTheme, bool readerSelectedDuringReload)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness("Harness.gumj");
        ComponentSave styles = tree.Project.AddComponent("Styles");
        tree.Project.AddInstance(styles, "Primary", "Rectangle");
        styles.DefaultState.SetValue("Primary.FillRed", 21, "int");
        ComponentSave? theme = null;
        if (viaTheme)
        {
            theme = tree.Project.AddComponent("Theme");
            tree.Project.AddInstance(theme, "Primary", "Rectangle");
            theme.DefaultState.SetValue("Primary.FillRed", 21, "int");
            theme.DefaultState.VariableLists.Add(new VariableListSave<string>
            {
                Name = "Primary.VariableReferences",
                Type = "string",
                Value = new List<string> { "FillRed = Components/Styles.Primary.FillRed" },
            });
        }
        ComponentSave button = tree.Project.AddComponent("ButtonStandard");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        button.DefaultState.SetValue("Background.Red", 21, "int");
        button.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences",
            Type = "string",
            Value = new List<string> { "Red = Components/" + (viaTheme ? "Theme" : "Styles") + ".Primary.FillRed" },
        });
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        string stylesFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Styles.gucj");
        string themeFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Theme.gucj");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonStandard.gucj");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = true;
            tree.SaveAll();

            // An in-tool edit of each reader gives it an undo history that holds the old value.
            if (theme != null)
            {
                tree.Click(tree.NodeFor(theme));
                tree.Grid.TypeAndEnter("Width", "60");
            }
            tree.Click(tree.NodeFor(button));
            tree.Grid.TypeAndEnter("Width", "80");
            if (!readerSelectedDuringReload)
            {
                tree.Click(tree.NodeFor(styles));
            }

            File.WriteAllText(stylesFile, Regex.Replace(File.ReadAllText(stylesFile),
                @"(""Name"": ""Primary\.FillRed"",\s*""Type"": ""int"",\s*""SetsValue"": true,\s*""ValueAsInt"": )\d+", "${1}200"));
            fileChanges.ReactToFileChanged(new FilePath(stylesFile));
            tree.ThrowIfCrashed();

            if (theme != null)
            {
                tree.Click(tree.NodeFor(Component(tree, "Theme")));
                tree.Undo();
                VariableGridHarness.StoredValue(Component(tree, "Theme"), "Width").ShouldBeNull();
                VariableGridHarness.StoredValue(Component(tree, "Theme"), "Primary.FillRed").ShouldBe(200);
                ReadFillRed<ComponentSave>(themeFile, "Primary.FillRed").ShouldBe(200);
            }
            tree.Click(tree.NodeFor(Component(tree, "ButtonStandard")));
            tree.Undo();

            // The edit undid, and the value the reload gave the reader stayed, in memory and on disk.
            ComponentSave undone = Component(tree, "ButtonStandard");
            VariableGridHarness.StoredValue(undone, "Width").ShouldBeNull();
            VariableGridHarness.StoredValue(undone, "Background.Red").ShouldBe(200);
            ReadFillRed<ComponentSave>(buttonFile, "Background.Red").ShouldBe(200);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    // A reference row added in-tool (and so undoable), then an outside reload: undoing the row removes the
    // row and the value it produced, as it does without the reload.
    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    [Trait("Feature", "VARREF-001")]
    public void UndoingAReferenceRowAddedBeforeAnOutsideReload_RemovesTheRowAndItsValue()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness("Harness.gumj");
        ComponentSave styles = tree.Project.AddComponent("Styles");
        tree.Project.AddInstance(styles, "Primary", "Rectangle");
        styles.DefaultState.SetValue("Primary.FillRed", 21, "int");
        ComponentSave button = tree.Project.AddComponent("ButtonStandard");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        string stylesFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Styles.gucj");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonStandard.gucj");

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = true;
            tree.SaveAll();
            tree.Click(tree.NodeFor(button));
            using (tree.Project.UndoManager.RequestLock())
            {
                button.DefaultState.SetValue("Background.Red", 21, "int");
                button.DefaultState.VariableLists.Add(new VariableListSave<string>
                {
                    Name = "Background.VariableReferences",
                    Type = "string",
                    Value = new List<string> { "Red = Components/Styles.Primary.FillRed" },
                });
            }
            TestAppBuilder.Services.GetRequiredService<IFileCommands>().TryAutoSaveElement(button);

            File.WriteAllText(stylesFile, Regex.Replace(File.ReadAllText(stylesFile),
                @"(""Name"": ""Primary\.FillRed"",\s*""Type"": ""int"",\s*""SetsValue"": true,\s*""ValueAsInt"": )\d+", "${1}200"));
            fileChanges.ReactToFileChanged(new FilePath(stylesFile));
            tree.ThrowIfCrashed();
            VariableGridHarness.StoredValue(Component(tree, "ButtonStandard"), "Background.Red").ShouldBe(200);

            tree.Undo();

            ComponentSave undone = Component(tree, "ButtonStandard");
            undone.DefaultState.VariableLists.ShouldBeEmpty();
            VariableGridHarness.StoredValue(undone, "Background.Red").ShouldBeNull();
            ComponentSave onDisk = ElementReference.DeserializeElement<ComponentSave>(buttonFile, GumProjectSave.NativeVersion);
            onDisk.DefaultState.VariableLists.ShouldBeEmpty();
            VariableGridHarness.StoredValue(onDisk, "Background.Red").ShouldBeNull();
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    private static object? ReadFillRed<T>(string file, string variableName) where T : ElementSave, new() =>
        VariableGridHarness.StoredValue(ElementReference.DeserializeElement<T>(file, GumProjectSave.NativeVersion), variableName);

    // The ignore window hides the tool's own write from the watcher. A different write to the same file
    // inside that window is an outside edit and must still reach the reload (#6866).
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "FILE-013")]
    public void AnOutsideEditShortlyAfterAToolSave_IsNotIgnored(bool savedByProjectSave)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        IFileWatchIgnoreList ignoreList = TestAppBuilder.Services.GetRequiredService<IFileWatchIgnoreList>();
        FilePath buttonFile = new FilePath(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx"));

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = true;
            if (savedByProjectSave)
            {
                tree.SaveAll();
            }
            else
            {
                tree.Grid.TypeAndEnter("Width", "80");
            }

            // The tool's own write is still ignored, including events that arrive after it finished.
            ignoreList.TryGetIgnoreFileChange(buttonFile).ShouldBeTrue();

            string toolBytes = File.ReadAllText(buttonFile.FullPath);
            File.WriteAllText(buttonFile.FullPath, toolBytes + "<!-- outside -->");

            ignoreList.TryGetIgnoreFileChange(buttonFile).ShouldBeFalse();

            // The same bytes again are the tool's file; leave it as the tool wrote it for the oracles.
            File.WriteAllText(buttonFile.FullPath, toolBytes);
            ignoreList.TryGetIgnoreFileChange(buttonFile).ShouldBeTrue();
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Feature", "FILE-013")]
    public void AnOutsideEditShortlyAfterAToolBehaviorSave_IsNotIgnored(bool savedByProjectSave)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        IFileWatchIgnoreList ignoreList = TestAppBuilder.Services.GetRequiredService<IFileWatchIgnoreList>();
        FilePath behaviorFile = new FilePath(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx"));

        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = true;
            if (savedByProjectSave)
            {
                tree.SaveAll();
            }
            else
            {
                tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = "Looks"; return true; });
                tree.States.ClickNewCategory();
            }

            ignoreList.TryGetIgnoreFileChange(behaviorFile).ShouldBeTrue();

            string toolBytes = File.ReadAllText(behaviorFile.FullPath);
            File.WriteAllText(behaviorFile.FullPath, toolBytes + "<!-- outside -->");

            ignoreList.TryGetIgnoreFileChange(behaviorFile).ShouldBeFalse();

            File.WriteAllText(behaviorFile.FullPath, toolBytes);
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "FILE-013")]
    public void AnOutsideEditShortlyAfterAToolProjectFileSave_IsNotIgnored()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        IFileWatchIgnoreList ignoreList = TestAppBuilder.Services.GetRequiredService<IFileWatchIgnoreList>();
        tree.SaveAll();
        FilePath projectFile = new FilePath(tree.Project.Project.FullFileName);

        ignoreList.TryGetIgnoreFileChange(projectFile).ShouldBeTrue();

        string toolBytes = File.ReadAllText(projectFile.FullPath);
        File.WriteAllText(projectFile.FullPath, toolBytes + "<!-- outside -->");

        ignoreList.TryGetIgnoreFileChange(projectFile).ShouldBeFalse();

        File.WriteAllText(projectFile.FullPath, toolBytes);
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
