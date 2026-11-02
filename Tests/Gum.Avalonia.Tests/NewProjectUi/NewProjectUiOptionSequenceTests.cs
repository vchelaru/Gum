using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The New Project dialog's derived state (command line, Folders tree, project path, notes) depends only on the final
/// choices, not on the order the options were toggled in. Each sequence is clicked through the real controls and
/// compared to a fresh dialog whose view model was set to the same final choices directly.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiOptionSequenceTests
{
    private const string Theme = "Bubblegum";

    // One step of a sequence, made with the dialog's controls.
    private static void Apply(NewProjectUiFixture fixture, string step)
    {
        NewProjectDialogDriver dialog = fixture.Dialog;
        fixture.Settle();
        switch (step)
        {
            case "code":
                dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
                break;
            case "codeOff":
                dialog.SetChecked(NewProjectUiText.CreateCodeProject, false);
                break;
            case "formsOff":
                dialog.SetChecked(NewProjectUiText.IncludeForms, false);
                break;
            case "formsOn":
                dialog.SetChecked(NewProjectUiText.IncludeForms, true);
                break;
            case "theme":
                dialog.ChooseTheme(Theme);
                break;
            case "xml":
                dialog.ChooseFileFormat(NewProjectUiText.XmlFormat);
                break;
            case "json":
                dialog.ChooseFileFormat(NewProjectUiText.JsonFormat);
                break;
            case "noRestore":
                dialog.SetChecked(NewProjectUiText.RestorePackages, false);
                break;
            case "demo":
                dialog.SetChecked(NewProjectUiText.IncludeDemo, true);
                break;
            default:
                dialog.ChoosePlatform(step.Substring("platform:".Length));
                break;
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        // steps, the final choices (as view model settings), a name for the report
        yield return new object[] { "code,platform:raylib,xml,theme", "code=1;platform=raylib;xml=1;theme=1" };
        yield return new object[] { "theme,xml,code,platform:kniskia", "code=1;platform=kniskia;xml=1;theme=1" };
        yield return new object[] { "xml,code,platform:stride,formsOff,formsOn", "code=1;platform=stride;xml=1" };
        yield return new object[] { "theme,formsOff,code,platform:silknet", "code=1;platform=silknet;forms=0;theme=1" };
        yield return new object[] { "code,platform:kni,formsOff,formsOn,theme,xml,json", "code=1;platform=kni;theme=1" };
        yield return new object[] { "code,platform:raylib,codeOff", "" };
        yield return new object[] { "code,platform:raylib,noRestore,codeOff,code", "code=1;platform=raylib;restore=0" };
        yield return new object[] { "demo,formsOff,formsOn", "demo=1" };
        yield return new object[] { "theme,code,platform:kniskia,platform:monogame,formsOff", "code=1;platform=monogame;forms=0;theme=1" };
        yield return new object[] { "xml,theme,formsOff,code,platform:raylib,formsOn,json", "code=1;platform=raylib;theme=1" };
    }

    private static void SetDirectly(NewProjectDialogViewModel viewModel, string choices)
    {
        foreach (string choice in choices.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = choice.Split('=');
            switch (parts[0])
            {
                case "code":
                    viewModel.IncludeCodeProject = parts[1] == "1";
                    break;
                case "platform":
                    viewModel.PlatformName = parts[1];
                    break;
                case "xml":
                    viewModel.SelectedFileFormat = NewProjectUiText.XmlFormat;
                    break;
                case "theme":
                    viewModel.ThemeSelection.SelectedTheme = Theme;
                    break;
                case "forms":
                    viewModel.IsIncludeFormsControls = parts[1] == "1";
                    break;
                case "restore":
                    viewModel.RunRestore = parts[1] == "1";
                    break;
                case "demo":
                    viewModel.IsIncludeDemoScreenGum = parts[1] == "1";
                    break;
            }
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void AnyOrderOfToggles_EndsInTheSameStateAsSettingTheFinalChoicesDirectly(string steps, string finalChoices)
    {
        string viaControls;
        using (NewProjectUiFixture clicked = new NewProjectUiFixture())
        {
            foreach (string step in steps.Split(','))
            {
                Apply(clicked, step);
            }
            clicked.Settle();
            viaControls = clicked.Snapshot();
        }

        string direct;
        using (NewProjectUiFixture reference = new NewProjectUiFixture())
        {
            SetDirectly(reference.ViewModel, finalChoices);
            reference.Settle();
            direct = reference.Snapshot();
        }

        viaControls.ShouldBe(direct, $"after {steps}");
    }

    [AvaloniaFact]
    public void ThemeChosenBeforeFormsIsUntickedIsRememberedWhenFormsComesBack_AndIgnoredWhileOff()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.ChooseTheme(Theme);
        fixture.Dialog.OpenCommandLine().ShouldContain($"--theme {Theme}");

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        fixture.Dialog.CommandLine.ShouldNotContain("--theme");
        fixture.Dialog.CommandLine.ShouldContain("--template empty");
        fixture.Dialog.ThemeBox.IsEffectivelyVisible.ShouldBeFalse();
        fixture.Dialog.CheckBoxLabeled(NewProjectUiText.IncludeDemo).IsEffectivelyVisible.ShouldBeFalse();

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, true);
        fixture.Dialog.ThemeBox.SelectedItem.ShouldBe(Theme);
        fixture.Dialog.CommandLine.ShouldContain($"--theme {Theme}");
        fixture.Dialog.CommandLine.ShouldNotContain("--template");
    }

    [AvaloniaFact]
    public void SwitchingTheFileFormatWithACodeProject_ChangesTheProjectFile_AndTheNoteExplainsTheCommandCannot()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.OpenCommandLine();
        fixture.Dialog.ChooseFileFormat(NewProjectUiText.XmlFormat);

        fixture.ViewModel.GumProjectPath.ShouldEndWith(Path.Combine("Content", "GumProject", "GumProject.gumx"));
        fixture.Settle();
        fixture.IsShownContaining(NewProjectUiText.NoXmlWithCodeProjectNote).ShouldBeTrue("the command line has no way to ask for .gumx with a code project");
        fixture.OpenSection(Gum.Avalonia.Dialogs.Views.NewProjectDialogView.FoldersExpanderName);
        fixture.ViewModel.SetFolderTreeExpanded(true);
        fixture.TreeRowTexts().ShouldContain("GumProject.gumx");
        fixture.TreeRowTexts().ShouldNotContain("GumProject.gumj");

        fixture.Dialog.ChooseFileFormat(NewProjectUiText.JsonFormat);
        fixture.Settle();
        fixture.IsShownContaining(NewProjectUiText.NoXmlWithCodeProjectNote).ShouldBeFalse();
        fixture.TreeRowTexts().ShouldContain("GumProject.gumj");
    }

    [AvaloniaFact]
    public void ACodeProjectInXml_CreatesTheGumxTheFoldersTreeMarked()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChooseFileFormat(NewProjectUiText.XmlFormat);
        List<string> folders = new List<string>();
        List<string> mainFiles = new List<string>();
        NewProjectUiFixture.CollectLayout(fixture.ViewModel.FolderTree.Single(), folders, mainFiles);

        fixture.Dialog.ClickOk();

        mainFiles.ShouldBe(new[] { "Content/GumProject/GumProject.gumx" });
        File.Exists(Path.Combine(fixture.GameFolder("Game"), mainFiles.Single())).ShouldBeTrue();
        Path.GetFileName(fixture.ViewModel.CreatedGumProjectPath!).ShouldBe("GumProject.gumx");
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Content", "GumProject", "GumProject.gumj")).ShouldBeFalse("no second project in the other format");
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADemoScreenTickedBeforeFormsWasUnticked_IsNotCreatedInTheCodeProject(bool formsOff)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeDemo, true);
        if (formsOff)
        {
            fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        }

        fixture.Dialog.ClickOk();

        string gumFolder = Path.Combine(fixture.GameFolder("Game"), "Content", "GumProject");
        string[] demoFiles = Directory.GetFiles(gumFolder, "DemoScreenGum.*", SearchOption.AllDirectories);
        if (formsOff)
        {
            demoFiles.ShouldBeEmpty("the demo screen needs the Forms controls it shows");
            Directory.GetFiles(gumFolder, "*", SearchOption.AllDirectories).Where(file => file.Contains(Path.Combine("Components", "Controls"))).ShouldBeEmpty();
        }
        else
        {
            demoFiles.ShouldNotBeEmpty();
        }
    }
}
