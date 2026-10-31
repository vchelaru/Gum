using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The Include recommended packages check box (#7265) in the real dialog window: it shows only with a C# project,
/// drives the command line, and OK adds the packages (through the recording runner, never a real <c>dotnet add</c>).
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiRecommendedPackagesTests
{
    private static CheckBox PackagesBox(NewProjectUiFixture fixture) =>
        fixture.Window.Find<CheckBox>(box => box.Name == NewProjectDialogView.RecommendedPackagesCheckBoxName);

    private static RecordingPackageAddRunner Runner => TestAppBuilder.Services.GetRequiredService<RecordingPackageAddRunner>();

    private static NewProjectUiFixture StartCodeProject(string platform = "monogame")
    {
        Runner.Reset();
        NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform(platform);
        fixture.Settle();
        return fixture;
    }

    [AvaloniaFact]
    public void TheCheckBox_IsHiddenWithoutACodeProject_AndShownWithOne()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();

        PackagesBox(fixture).IsEffectivelyVisible.ShouldBeFalse();

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Settle();

        PackagesBox(fixture).IsEffectivelyVisible.ShouldBeTrue();
        PackagesBox(fixture).Content.ShouldBe(NewProjectUiText.RecommendedPackages);
    }

    [AvaloniaFact]
    public void TheCheckBox_ShowsCheckedByDefault_AndTheNoteListsThePackages()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.ViewModel.IncludeRecommendedPackages = true;
        fixture.Settle();

        PackagesBox(fixture).IsChecked.ShouldBe(true);
        fixture.IsShown("Adds KernSmith.MonoGameGum, Gum.Expressions to the C# project, as the setup docs suggest.").ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TickingTheBox_ChainsTheAddsOnTheCommandLine_AndUntickingRestoresThePlainLine()
    {
        using NewProjectUiFixture fixture = StartCodeProject("stride");
        fixture.Dialog.OpenCommandLine();

        fixture.Dialog.SetChecked(NewProjectUiText.RecommendedPackages, true);
        fixture.Settle();
        fixture.Dialog.CommandLine.ShouldContain("--platform stride --no-restore && dotnet add \"");
        fixture.Dialog.CommandLine.ShouldContain("package Gum.Expressions --no-restore && dotnet restore \"");

        fixture.Dialog.SetChecked(NewProjectUiText.RecommendedPackages, false);
        fixture.Settle();
        fixture.Dialog.CommandLine.ShouldNotContain("dotnet");
    }

    [AvaloniaFact]
    public void ThePlatformWithoutPackages_DisablesTheBox()
    {
        using NewProjectUiFixture fixture = StartCodeProject("kniskia");

        PackagesBox(fixture).IsEnabled.ShouldBeFalse();
        PackagesBox(fixture).IsChecked.ShouldBe(false);
    }

    [AvaloniaFact]
    public void Ok_WithTheBoxTicked_AddsThePlatformsPackages_ThenCloses()
    {
        using NewProjectUiFixture fixture = StartCodeProject("raylib");
        fixture.ViewModel.IncludeRecommendedPackages = true;
        fixture.ViewModel.RunRestore = false;
        fixture.Settle();

        fixture.Dialog.ClickOk();

        Runner.Added.ShouldBe(new[] { "KernSmith.RaylibGum", "Gum.Expressions" });
    }

    [AvaloniaFact]
    public void Ok_WithTheBoxUnticked_AddsNothing()
    {
        using NewProjectUiFixture fixture = StartCodeProject("raylib");
        fixture.ViewModel.RunRestore = false;
        fixture.Settle();

        fixture.Dialog.ClickOk();

        Runner.Added.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void AFailedAdd_ShowsTheFailurePanelWithItsButtons_AndRetryFinishes()
    {
        using NewProjectUiFixture fixture = StartCodeProject("stride");
        fixture.ViewModel.IncludeRecommendedPackages = true;
        fixture.ViewModel.RunRestore = false;
        fixture.Settle();
        bool fail = true;
        Runner.Result = _ => fail
            ? new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "error: cannot write the csproj" }
            : new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

        fixture.Dialog.ClickOkWithoutWaitingForClose();
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the failure panel");

        fixture.IsShown(NewProjectDialogText.PackagesFailedHeading).ShouldBeTrue();
        fixture.IsShown("Could not add Gum.Expressions. error: cannot write the csproj").ShouldBeTrue();
        fixture.ButtonLabeled("Retry").ShouldNotBeNull();
        fixture.ButtonLabeled("Back").ShouldNotBeNull();

        fail = false;
        fixture.Dialog.ClickButton("Retry");
        fixture.WaitUntil(() => fixture.Dialog.Result == true, "the dialog to close");
    }
}
