using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Dialogs;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the tool's dialogs (inventory area DLG) answered in the head's own dialog
/// window and view (<see cref="ScriptedDialogService.AnswerNextInWindow{T}"/>): the gesture that
/// opens the dialog is real, and so is the input that answers it (typing, clicks, Enter, access keys),
/// so a view that loses a binding fails here even when its view model is right.
/// </summary>
[Trait("Category", "EndToEnd")]
public class DialogWindowScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    [Trait("Feature", "DLG-007")]
    public void RenameFolder_TypingTheNewNameAndPressingEnter_MovesTheFolder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Controls/Toggle");
        string? title = null;
        tree.Dialogs.AnswerNextInWindow<RenameFolderDialogViewModel>(window =>
        {
            title = window.Title;
            window.TypeInto(window.Find<TextBox>(), "Widgets");
            window.Press(Key.Enter, PhysicalKey.Enter);
        });

        tree.RightClick(tree.FolderNode("Components", "Controls"));
        tree.PickMenu("Rename Folder");

        title.ShouldNotBeNullOrEmpty();
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Widgets/Toggle" });
        tree.ChildTexts(tree.FolderNode("Components", "Widgets")).ShouldBe(new[] { "Toggle" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Widgets", "Toggle.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-008")]
    public void CreateComponent_KeepingTheOfferedName_AndTickingTheCheckbox_ReplacesTheInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Box", "Container");
        tree.SaveAll();
        tree.Click(tree.NodeFor(button.Instances.Single()));
        string offeredName = "";
        tree.Dialogs.AnswerNextInWindow<CreateComponentDialogViewModel>(window =>
        {
            offeredName = window.Find<TextBox>().Text ?? "";
            window.Click(window.Find<CheckBox>(box => box.IsEffectivelyVisible));
            window.Click(window.AffirmativeButton);
        });

        tree.RightClick(tree.NodeFor(button.Instances.Single()));
        tree.PickMenu("Create Component");

        offeredName.ShouldBe("BoxComponent");
        tree.Project.Project.Components.Select(component => component.Name).ShouldContain("BoxComponent");
        tree.Project.Project.Components.Single(component => component.Name == "Button").Instances.Single().BaseType.ShouldBe("BoxComponent");
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "BoxComponent.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-010")]
    public void DeleteDialog_NKeepsTheComponent_AndYDeletesIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.SaveAll();
        string cardFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextInWindow<DeleteOptionsDialogViewModel>(window => window.Press(Key.N, PhysicalKey.N, symbol: "n"));
        tree.RightClick(tree.NodeFor(Card(tree)));
        tree.PickMenu("Delete");
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Card" }, "N answers no");
        tree.SnapshotFiles().ShouldMatch(start, "declining a delete changes no file");

        tree.Dialogs.AnswerNextInWindow<DeleteOptionsDialogViewModel>(window => window.Press(Key.Y, PhysicalKey.Y, symbol: "y"));
        tree.RightClick(tree.NodeFor(Card(tree)));
        tree.PickMenu("Delete");

        tree.Project.Project.Components.ShouldBeEmpty("Y answers yes");
        tree.RootNode("Components").Nodes.ShouldBeEmpty();
        File.Exists(cardFile).ShouldBeFalse();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-011")]
    public void ViewReferences_ClickingAListedReference_SelectsIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.SaveAll();
        ComponentSave button = tree.Project.Project.Components.Single(component => component.Name == "Button");
        tree.Click(tree.NodeFor(button));
        string shown = "";
        tree.Dialogs.AnswerNextInWindow<DisplayReferencesDialog>(window =>
        {
            shown = window.Text();
            window.Click(window.Find<ListBoxItem>());
            window.Click(window.AffirmativeButton);
        });

        tree.RightClick(tree.NodeFor(button));
        tree.PickMenu("View References");

        shown.ShouldContain("The following files reference Button");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(okButton, "clicking the reference selects the instance that makes it");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-012")]
    [Trait("Feature", "EDIT-001")]
    public void ExposeColor_TypingABaseName_ExposesTheThreeChannelsUnderIt_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.SaveAll();
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();
        tree.Dialogs.AnswerNextInWindow<ExposeColorDialogViewModel>(window =>
        {
            window.TypeInto(window.Find<TextBox>(), "Tint");
            window.Click(window.AffirmativeButton);
        });

        tree.Grid.PickRowMenuItem("Color", "Expose Color");

        button.GetDefaultStateOrThrow().Variables
            .Where(variable => variable.Name is "Label.Red" or "Label.Green" or "Label.Blue")
            .Select(variable => variable.ExposedAsName).OrderBy(name => name)
            .ShouldBe(new[] { "TintBlue", "TintGreen", "TintRed" });

        tree.Click(tree.NodeFor(button));
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the exposure restores the files");
        tree.Redo();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-016")]
    public void AddVariable_ClickingATypeAndTypingAName_AddsItToTheBehavior()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        tree.Click(tree.RootNode("Behaviors").Nodes.Single());
        tree.Dialogs.AnswerNextInWindow<AddVariableViewModel>(window =>
        {
            window.Click(window.Find<ListBoxItem>(item => item.Content as string == "bool"));
            window.TypeInto(window.Find<TextBox>(), "IsEnabled");
            window.Click(window.AffirmativeButton);
        });

        tree.Grid.Input.Click(tree.Grid.View.AddVariableButton);
        tree.Grid.Settle();

        Gum.DataTypes.Variables.VariableSave added = tree.Project.Project.Behaviors.Single().RequiredVariables.Variables.ShouldHaveSingleItem();
        (added.Type, added.Name).ShouldBe(("bool", "IsEnabled"));
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldContain("IsEnabled");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-003")]
    public void SavingAReadOnlyElementFile_OffersChoices_AndOpenFolderRevealsTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.SaveAll();
        tree.Click(tree.NodeFor(card));
        ProjectFileSnapshot start = tree.SnapshotFiles();
        string cardFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Card.gucx");
        RecordingFileSystemRevealService reveal = (RecordingFileSystemRevealService)Services.GetRequiredService<IFileSystemRevealService>();
        reveal.Clear();
        List<string> offered = new List<string>();
        tree.Dialogs.AnswerNextInWindow<ChoiceDialogViewModel>(window =>
        {
            offered.AddRange(window.FindAll<ListBoxItem>().Select(item => item.Content as string ?? ""));
            window.Click(window.Find<ListBoxItem>(item => item.Content as string == "Open folder containing file"));
            window.Click(window.AffirmativeButton);
        });
        File.SetAttributes(cardFile, File.GetAttributes(cardFile) | FileAttributes.ReadOnly);
        try
        {
            tree.Grid.TypeAndEnter("Width", "175");
        }
        finally
        {
            File.SetAttributes(cardFile, File.GetAttributes(cardFile) & ~FileAttributes.ReadOnly);
        }

        offered.Count.ShouldBe(2);
        offered.ShouldContain("Open folder containing file");
        reveal.Requests.ShouldBe(new[] { "Reveal: " + cardFile });
        tree.SnapshotFiles().ShouldMatch(start, "a read-only file is not written");

        // Undo leaves the file as it was; the oracles then check memory and disk agree again.
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undo writes the file back as it was");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-020")]
    public void FreezeDiagnosticsPrompt_AfterADirtyExitWithADump_OpensTheFolderOnce_AndDoNotAskAgainSilencesIt()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumFreezeDiagnosticsScenario", Guid.NewGuid().ToString("N"));
        ScriptedDialogService dialogs = new ScriptedDialogService();
        RecordingFileSystemRevealService reveal = (RecordingFileSystemRevealService)Services.GetRequiredService<IFileSystemRevealService>();
        reveal.Clear();
        try
        {
            // A launch that froze: the session never ended cleanly, and the watchdog left a dump.
            new FreezeDiagnosticsInbox(folder).BeginSession().ShouldBeFalse();
            string dump = Path.Combine(folder, "freeze-1.txt");
            File.WriteAllText(dump, "stack");
            string message = "";
            dialogs.AnswerNextInWindow<FreezeDiagnosticsPromptViewModel>(window =>
            {
                message = window.Text();
                window.Click(window.AffirmativeButton);
            });

            Launch(folder, dialogs);

            message.ShouldContain("1 diagnostic files");
            reveal.Requests.ShouldBe(new[] { "OpenFolder: " + folder });
            File.Exists(dump).ShouldBeFalse("the reported dump moves out of the inbox");
            File.Exists(Path.Combine(folder, "Reported", "freeze-1.txt")).ShouldBeTrue();

            // The next freeze: this time the user ticks "Don't ask again" and closes the prompt.
            File.WriteAllText(Path.Combine(folder, "freeze-2.txt"), "stack");
            dialogs.AnswerNextInWindow<FreezeDiagnosticsPromptViewModel>(window =>
            {
                window.Click(window.Find<CheckBox>());
                window.Press(Key.Escape, PhysicalKey.Escape);
            });
            Launch(folder, dialogs);
            reveal.Requests.Count.ShouldBe(1, "closing the prompt opens nothing");

            // A third freeze prompts no more: an unanswered dialog would fail the test.
            File.WriteAllText(Path.Combine(folder, "freeze-3.txt"), "stack");
            Launch(folder, dialogs);
            File.Exists(Path.Combine(folder, "freeze-3.txt")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // What App does at startup: begin the session (reporting whether the last one ended dirty),
    // then prompt with the head's dialog service; the session is left running, as when the tool is killed.
    private static void Launch(string folder, IDialogService dialogs)
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(folder);
        bool previousSessionEndedDirty = inbox.BeginSession();
        new FreezeDiagnosticsPromptService(inbox, dialogs, Services.GetRequiredService<IFileSystemRevealService>())
            .PromptIfNeeded(previousSessionEndedDirty);
    }

    private static ComponentSave Card(ProjectTreeHarness tree) =>
        tree.Project.Project.Components.Single(component => component.Name == "Card");
}
