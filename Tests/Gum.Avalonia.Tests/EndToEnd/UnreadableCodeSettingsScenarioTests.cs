using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A ProjectCodeSettings.codsj that does not parse is the user's settings, so the Code tab shows the
/// parse error and saves nothing over it until the file is fixed on disk (which reloads it) or the
/// user resets it to defaults (#6600).
/// </summary>
[Trait("Category", "EndToEnd")]
public class UnreadableCodeSettingsScenarioTests
{
    private const string Unparsable = "<<<<<<< HEAD\n{ \"RootNamespace\": \"Mine\"\n=======";
    private const string AutoReloadLabel = "Changes to ProjectCodeSettings.codsj are reloaded automatically";

    private static string SettingsFile(CodeTabHarness code) => Path.Combine(code.Project.ProjectFolder, "ProjectCodeSettings.codsj");

    private static CodeTabHarness OpenWithUnparsableSettings()
    {
        CodeTabHarness code = new CodeTabHarness(folder => File.WriteAllText(Path.Combine(folder, "ProjectCodeSettings.codsj"), Unparsable));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        return code;
    }

    private static List<string> ShownTexts(CodeTabHarness code)
    {
        code.Input.Layout();
        return code.View.GetVisualDescendants().OfType<TextBlock>()
            .Where(block => block.IsEffectivelyVisible && !string.IsNullOrEmpty(block.Text))
            .Select(block => block.Text!)
            .ToList();
    }

    [AvaloniaFact]
    public void ValidSettingsFile_LoadsEditable()
    {
        using CodeTabHarness code = new CodeTabHarness(folder => File.WriteAllText(Path.Combine(folder, "ProjectCodeSettings.codsj"),
            """{ "CodeProjectRoot": "Code/", "RootNamespace": "FromDisk", "Version": 1 }"""));

        code.ViewModel.ProjectSettingsLoadError.ShouldBeNull();
        code.Member("Root Namespace").Value.ShouldBe("FromDisk");
        code.Member("Root Namespace").IsReadOnly.ShouldBeFalse();
        ShownTexts(code).ShouldNotContain(AutoReloadLabel);
    }

    [AvaloniaFact]
    public void MissingSettingsFile_UsesDefaults_AndAnEditCreatesIt()
    {
        using CodeTabHarness code = new CodeTabHarness();
        File.Exists(SettingsFile(code)).ShouldBeFalse();

        code.ViewModel.ProjectSettingsLoadError.ShouldBeNull();
        code.ShowsButton("Reset to defaults").ShouldBeFalse();
        code.Member("Root Namespace").IsReadOnly.ShouldBeFalse();

        code.TypeAndEnter("Root Namespace", "Created");
        File.ReadAllText(SettingsFile(code)).ShouldContain("\"RootNamespace\": \"Created\"");
    }

    [AvaloniaFact]
    public void UnparsableSettingsFile_ShowsTheParseError_AndNoEditSavesOverIt()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();

        code.ViewModel.ProjectSettingsLoadError.ShouldNotBeNull();
        List<string> texts = ShownTexts(code);
        texts.ShouldContain(AutoReloadLabel);
        texts.ShouldContain(text => text.Contains(code.ViewModel.ProjectSettingsLoadError!));
        code.ShowsButton("Reset to defaults").ShouldBeTrue();
        code.ShowsButton("Manual").ShouldBeFalse("setup would write the defaults over the file");
        code.Grid.IsEffectivelyVisible.ShouldBeFalse();
        code.View.GenerateButton.IsEffectivelyVisible.ShouldBeFalse();

        // The project-wide rows on the Settings window page are read-only, and a value pushed into
        // one anyway is not saved.
        code.Member("Root Namespace").IsReadOnly.ShouldBeTrue();
        code.Member("Root Namespace").Value = "Overwritten";
        code.ViewModel.InheritanceLocation = code.ViewModel.InheritanceLocation == Gum.ProjectServices.CodeGeneration.InheritanceLocation.InCustomCode
            ? Gum.ProjectServices.CodeGeneration.InheritanceLocation.InGeneratedCode
            : Gum.ProjectServices.CodeGeneration.InheritanceLocation.InCustomCode;

        File.ReadAllText(SettingsFile(code)).ShouldBe(Unparsable);
    }

    [AvaloniaFact]
    public void FixingTheFileOnDisk_ReloadsIt_AndTheTabBecomesEditable()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();

        // Still broken after a save: the error stays, with the new parse message.
        File.WriteAllText(SettingsFile(code), "{ \"RootNamespace\": ");
        fileChanges.ReactToFileChanged(new FilePath(SettingsFile(code)));
        code.ViewModel.ProjectSettingsLoadError.ShouldNotBeNull();

        File.WriteAllText(SettingsFile(code), """{ "CodeProjectRoot": "Code/", "RootNamespace": "Fixed", "Version": 1 }""");
        fileChanges.ReactToFileChanged(new FilePath(SettingsFile(code)));
        code.Input.Layout();

        code.ViewModel.ProjectSettingsLoadError.ShouldBeNull();
        ShownTexts(code).ShouldNotContain(AutoReloadLabel);
        code.Grid.IsEffectivelyVisible.ShouldBeTrue();
        code.Member("Root Namespace").Value.ShouldBe("Fixed");
        code.Member("Root Namespace").IsReadOnly.ShouldBeFalse();

        code.TypeAndEnter("Root Namespace", "EditedInGum");
        File.ReadAllText(SettingsFile(code)).ShouldContain("\"RootNamespace\": \"EditedInGum\"");
    }

    [AvaloniaFact]
    public void UnparsableSettingsFile_PreviewShowsNoCodeBuiltFromTheDefaults()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();

        code.Preview.ShouldNotContain("class Card");
        code.Preview.ShouldContain(code.ViewModel.ProjectSettingsLoadError!);
    }

    [AvaloniaFact]
    public void UnparsableSettingsFile_HeaderDoesNotOfferSetup()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();

        List<string> texts = ShownTexts(code);
        texts.ShouldNotContain("Code Generation Setup");
        texts.ShouldContain("Code Generation");
    }

    [AvaloniaFact]
    public void UnparsableSettingsFile_FolderPickersOnTheSettingsPageAreDisabled()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();
        code.Input.Layout();

        List<Button> pickers = code.ProjectSettingsPage.Grid.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Content as string == "...")
            .ToList();
        pickers.ShouldNotBeEmpty();
        pickers.ShouldAllBe(button => !button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void UnparsableAtLoad_AutomaticGenerationAndRenames_WriteNoCode()
    {
        using CodeTabHarness code = new CodeTabHarness(folder => File.WriteAllText(Path.Combine(folder, "ProjectCodeSettings.codsj"), Unparsable));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        // The element's own settings are readable and ask for generation on every change.
        File.WriteAllText(Path.Combine(code.Project.ProjectFolder, "Components", "Card.codsj"),
            Newtonsoft.Json.JsonConvert.SerializeObject(new Gum.ProjectServices.CodeGeneration.CodeOutputElementSettings
            {
                GenerationBehavior = Gum.ProjectServices.CodeGeneration.GenerationBehavior.GenerateAutomaticallyOnPropertyChange,
            }));
        code.Select(card);

        EditRenameAndAddInstance(code, card);

        CodeFilesUnder(code.Project.ProjectFolder).ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void UnreadableAfterAnEditOnDisk_AutomaticGenerationAndRenames_WriteNoCode()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration(instantiation: "Fully in Code (no loaded Gum Project)");
        code.PickComboItem("Generation Behavior", "GenerateAutomaticallyOnPropertyChange");
        if (Directory.Exists(code.CodeFolder))
        {
            Directory.Delete(code.CodeFolder, recursive: true);
        }

        File.WriteAllText(SettingsFile(code), Unparsable);
        TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(SettingsFile(code)));
        code.ViewModel.ProjectSettingsLoadError.ShouldNotBeNull();

        EditRenameAndAddInstance(code, card);

        CodeFilesUnder(code.Project.ProjectFolder).ShouldBeEmpty();
        File.Exists(Path.Combine(code.Project.ProjectFolder, "Components", "Panel.codsj"))
            .ShouldBeTrue("the element's own settings file still follows a rename");
    }

    [AvaloniaFact]
    public void UnreadableAfterAnEditOnDisk_AChangeThroughAReference_RegeneratesNothing()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave style = code.Project.AddComponent("Style");
        ComponentSave button = code.Project.AddComponent("Button");
        code.Select(style);
        code.Tree.Grid.TypeAndEnter("Width", "90");
        code.Select(button);
        code.Tree.Grid.TypeLinesAndApply("VariableReferences", "Width = Components/Style.Width");
        code.Tree.SaveAll();
        // Button follows Style, so a Style edit reaches it through the reference.
        code.SetUpManualGeneration(instantiation: "Fully in Code (no loaded Gum Project)");
        code.PickComboItem("Generation Behavior", "GenerateAutomaticallyOnPropertyChange");
        string buttonCode = code.CodeFile("Components/Button.Generated.cs");
        // The tool's startup sends a reference-driven change to the plugins; the test host does not wire it.
        Action<ElementSave, InstanceSave?, string, object?, bool> sendToPlugins =
            (element, instance, name, oldValue, isFullCommit) =>
                TestAppBuilder.Services.GetRequiredService<IPluginManager>().VariableSetThroughReference(element, instance, name, oldValue, isFullCommit);
        GumRuntime.ElementSaveExtensions.VariableChangedThroughReference += sendToPlugins;
        try
        {
            // Readable: the reference regenerates Button, though Style is the element being edited.
            DeleteCodeFolder(code);
            EditStyleWidth(code, style, "120");
            File.Exists(buttonCode).ShouldBeTrue("a change through a reference regenerates the element that takes it");

            DeleteCodeFolder(code);
            BreakSettingsFileOnDisk(code);
            EditStyleWidth(code, style, "150");

            CodeFilesUnder(code.Project.ProjectFolder).ShouldBeEmpty();
        }
        finally
        {
            GumRuntime.ElementSaveExtensions.VariableChangedThroughReference -= sendToPlugins;
        }
    }

    private static void DeleteCodeFolder(CodeTabHarness code)
    {
        if (Directory.Exists(code.CodeFolder))
        {
            Directory.Delete(code.CodeFolder, recursive: true);
        }
    }

    private static void BreakSettingsFileOnDisk(CodeTabHarness code)
    {
        File.WriteAllText(SettingsFile(code), Unparsable);
        TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(SettingsFile(code)));
        code.ViewModel.ProjectSettingsLoadError.ShouldNotBeNull();
    }

    private static void EditStyleWidth(CodeTabHarness code, ComponentSave style, string width)
    {
        code.Select(style);
        code.Tree.Grid.TypeAndEnter("Width", width);
        // Elements the reference reached are generated after the commit, from the UI thread's queue.
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs(global::Avalonia.Threading.DispatcherPriority.Background);
    }

    [AvaloniaFact]
    public void UnreadableAfterAnEditOnDisk_ChangingBaseType_LeavesTheCustomCodeAlone()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration(instantiation: "Fully in Code (no loaded Gum Project)");
        code.ViewModel.InheritanceLocation = Gum.ProjectServices.CodeGeneration.InheritanceLocation.InCustomCode;
        code.ClickGenerate();
        string customFile = code.CodeFile("Components/Card.cs");
        string beforeChange = File.ReadAllText(customFile);

        // Readable: the custom code's declaration follows the new base type.
        code.Tree.Grid.PickComboItem("BaseType", "Rectangle");
        string afterReadableChange = File.ReadAllText(customFile);
        afterReadableChange.ShouldNotBe(beforeChange, "a base type change rewrites the custom code's declaration");

        BreakSettingsFileOnDisk(code);
        code.Tree.Grid.PickComboItem("BaseType", "Container");

        File.ReadAllText(customFile).ShouldBe(afterReadableChange);
    }

    // Each gesture here regenerates or moves an auto-generated element's code when the settings are readable.
    private static void EditRenameAndAddInstance(CodeTabHarness code, ComponentSave card)
    {
        code.Tree.Grid.TypeAndEnter("Width", "175");
        InstanceSave label = code.Project.AddInstance(card, "Label", "Text");
        code.Tree.Click(code.Tree.NodeFor(label));
        code.Tree.Grid.TypeAndEnter("X", "40");
        code.Tree.Grid.TypeAndEnter("Name", "Title");
        code.Project.Dialogs.AnswerNext<Gum.Dialogs.RenameElementDialogViewModel>(dialog => { dialog.Value = "Panel"; return true; });
        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        code.Tree.Click(code.Tree.NodeFor(card));
        code.Tree.Press(global::Avalonia.Input.Key.F2, global::Avalonia.Input.PhysicalKey.F2);
        card.Name.ShouldBe("Panel");
        // What an editor integration sends to generate every element.
        TestAppBuilder.Services.GetRequiredService<IMessenger>()
            .Send(new Gum.Messages.RequestCodeGenerationMessage());
    }

    private static List<string> CodeFilesUnder(string folder) =>
        Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories).ToList();

    [AvaloniaFact]
    public void ResetToDefaults_AfterConfirming_OverwritesTheFile_AndTheTabBecomesEditable()
    {
        using CodeTabHarness code = OpenWithUnparsableSettings();

        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
        code.ClickButton("Reset to defaults");
        File.ReadAllText(SettingsFile(code)).ShouldBe(Unparsable, "declining the confirmation keeps the file");
        code.ViewModel.ProjectSettingsLoadError.ShouldNotBeNull();

        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        code.ClickButton("Reset to defaults");

        code.ViewModel.ProjectSettingsLoadError.ShouldBeNull();
        File.ReadAllText(SettingsFile(code)).ShouldNotBe(Unparsable);
        Newtonsoft.Json.JsonConvert.DeserializeObject<Gum.ProjectServices.CodeGeneration.CodeOutputProjectSettings>(File.ReadAllText(SettingsFile(code)))
            .ShouldNotBeNull();
        code.ShowsButton("Reset to defaults").ShouldBeFalse();
        code.Member("Root Namespace").IsReadOnly.ShouldBeFalse();
    }
}
