using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
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
