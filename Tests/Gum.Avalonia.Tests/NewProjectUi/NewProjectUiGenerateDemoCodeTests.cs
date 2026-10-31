using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The Generate code for the demo screen check box (#7268) in the real dialog window: it shows only with a C# project that
/// has a demo screen, is checked by default, and chains the codegen step onto the command line.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiGenerateDemoCodeTests
{
    private static CheckBox GenerateBox(NewProjectUiFixture fixture) =>
        fixture.Window.Find<CheckBox>(box => box.Name == NewProjectDialogView.GenerateDemoCodeCheckBoxName);

    private static NewProjectUiFixture StartCodeProjectWithDemo()
    {
        NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeDemo, true);
        fixture.Settle();
        return fixture;
    }

    [AvaloniaFact]
    public void TheCheckBox_IsHiddenWithoutACodeProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeDemo, true);
        fixture.Settle();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void TheCheckBox_IsHiddenWithoutADemoScreen()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Settle();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void TheCheckBox_ShowsCheckedByDefault_WithACodeProjectAndTheDemo()
    {
        using NewProjectUiFixture fixture = StartCodeProjectWithDemo();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeTrue();
        GenerateBox(fixture).Content.ShouldBe(NewProjectUiText.GenerateDemoCode);
        GenerateBox(fixture).IsChecked.ShouldBe(true);
    }

    [AvaloniaFact]
    public void TheCheckBox_HidesAgain_WhenTheDemoIsUnticked()
    {
        using NewProjectUiFixture fixture = StartCodeProjectWithDemo();

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeDemo, false);
        fixture.Settle();

        GenerateBox(fixture).IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void UntickingTheBox_RemovesTheCodegenStepFromTheCommandLine()
    {
        using NewProjectUiFixture fixture = StartCodeProjectWithDemo();
        fixture.Dialog.OpenCommandLine();

        fixture.Dialog.CommandLine.ShouldContain(" && gumcli codegen \"");
        fixture.Dialog.CommandLine.ShouldEndWith("--element DemoScreenGum");

        fixture.Dialog.SetChecked(NewProjectUiText.GenerateDemoCode, false);
        fixture.Settle();
        fixture.Dialog.CommandLine.ShouldNotContain("codegen");
    }
}
