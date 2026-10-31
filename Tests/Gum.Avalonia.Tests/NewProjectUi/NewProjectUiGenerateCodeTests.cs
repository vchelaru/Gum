using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The Generate code check box (#7268) in the real dialog window: it shows with a C# project, is enabled when there is something
/// to generate, is checked by default, and chains the codegen step onto the command line.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiGenerateCodeTests
{
    private static CheckBox GenerateBox(NewProjectUiFixture fixture) =>
        fixture.Window.Find<CheckBox>(box => box.Name == NewProjectDialogView.GenerateCodeCheckBoxName);

    // The shared fixture starts with the box unticked so other tests keep their command lines; this one wants the default.
    private static NewProjectUiFixture StartCodeProject()
    {
        NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.ViewModel.GenerateCodeChoice = true;
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Settle();
        return fixture;
    }

    [AvaloniaFact]
    public void TheCheckBox_IsHiddenWithoutACodeProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void TheCheckBox_ShowsCheckedByDefault_WithACodeProject_EvenWithoutTheDemoScreen()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeDemo, false);
        fixture.Settle();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeTrue();
        GenerateBox(fixture).Content.ShouldBe(NewProjectUiText.GenerateCode);
        GenerateBox(fixture).IsChecked.ShouldBe(true);
        GenerateBox(fixture).IsEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TheCheckBox_IsDisabledAndUnticked_WithNothingToGenerate()
    {
        using NewProjectUiFixture fixture = StartCodeProject();

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        fixture.Settle();

        GenerateBox(fixture).IsEnabled.ShouldBeFalse();
        GenerateBox(fixture).IsChecked.ShouldBe(false);
        fixture.IsShown("Nothing to generate yet: the project has no screens or components. Include Forms controls to have some.").ShouldBeTrue();
    }

    [AvaloniaFact]
    public void UntickingTheBox_RemovesTheCodegenStepFromTheCommandLine()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.Dialog.OpenCommandLine();

        fixture.Dialog.CommandLine.ShouldEndWith(" && gumcli codegen \"" + fixture.ViewModel.GumProjectPath + "\"");

        fixture.Dialog.SetChecked(NewProjectUiText.GenerateCode, false);
        fixture.Settle();
        fixture.Dialog.CommandLine.ShouldNotContain("codegen");
    }
}
