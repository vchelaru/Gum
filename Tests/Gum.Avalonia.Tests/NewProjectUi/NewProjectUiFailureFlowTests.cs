using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The C# project's failure and busy flows through the real dialog window and the real file creation, with only the
/// restore (or file creation) stood in: the restore panel's three ways forward, Cancel and Escape while busy, and
/// errors that clear when an input changes.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiFailureFlowTests
{
    private const string RestoreFailedHeading = "The code project was created, but dotnet restore failed.";

    private static NewProjectUiFixture StartCodeProject(string name = "Game")
    {
        NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName(name);
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Settle();
        return fixture;
    }

    // Clicks OK and returns without waiting for the dialog to close.
    private static void StartCreation(NewProjectUiFixture fixture) => fixture.Window.Click(fixture.Dialog.OkButton);

    private static void FailRestoreOnce(NewProjectUiFixture fixture, out List<string> restored, string tail = "NU1101: no such package")
    {
        restored = fixture.StandInRestore(NewProjectUiFixture.RestoreFailed(tail), NewProjectUiFixture.RestoreOk());
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the restore failure panel");
    }

    // A restore that waits until it is canceled.
    private static void BlockRestore(NewProjectUiFixture fixture)
    {
        fixture.Service.Restore = async (_, _, token) =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
            }
            return new RestoreResult { Status = RestoreStatus.Canceled };
        };
    }

    private static void BlockCreation(NewProjectUiFixture fixture)
    {
        fixture.Service.CreateFiles = async (_, token) =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
            }
            return new NewProjectFilesResult { Canceled = true };
        };
    }

    [AvaloniaFact]
    public void ARestoreThatFails_ShowsThePanelWithItsThreeButtons_AndLocksTheForm()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out List<string> restored);

        fixture.IsShown(RestoreFailedHeading).ShouldBeTrue();
        fixture.IsShown("NU1101: no such package").ShouldBeTrue("the restore output tail shows");
        fixture.ButtonLabeled("Retry").ShouldNotBeNull();
        fixture.ButtonLabeled("Continue without restore").ShouldNotBeNull();
        fixture.ButtonLabeled("Back").ShouldNotBeNull();
        fixture.Dialog.NameBox.IsEffectivelyEnabled.ShouldBeFalse("the form is locked");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
        fixture.Dialog.CancelButton.IsEffectivelyEnabled.ShouldBeTrue();
        fixture.Dialog.Result.ShouldBeNull();
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue("the files were created before the restore");
        restored.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void Retry_RunsOnlyTheRestoreAgain_AndClosesAccepted()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out List<string> restored);
        string csproj = Path.Combine(fixture.GameFolder("Game"), "Game.csproj");
        DateTime written = File.GetLastWriteTimeUtc(csproj);

        fixture.ClickButton("Retry");
        fixture.WaitUntil(() => fixture.Dialog.Result != null, "the dialog to close");

        fixture.Dialog.Result.ShouldBe(true);
        restored.ShouldBe(new[] { csproj, csproj });
        File.GetLastWriteTimeUtc(csproj).ShouldBe(written, "the files were not written again");
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void RetryThatFailsAgain_StaysOnThePanel_WithTheNewOutput()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        List<string> restored = fixture.StandInRestore(
            NewProjectUiFixture.RestoreFailed("first failure"), NewProjectUiFixture.RestoreFailed("second failure"), NewProjectUiFixture.RestoreOk());
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the first failure");

        fixture.ClickButton("Retry");
        fixture.WaitUntil(() => fixture.ViewModel.RestoreFailureText == "second failure", "the second failure");

        fixture.Dialog.Result.ShouldBeNull();
        fixture.IsShown("second failure").ShouldBeTrue();
        fixture.IsShown("first failure").ShouldBeFalse();
        fixture.ButtonLabeled("Retry").ShouldNotBeNull("Retry is offered again");
        restored.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void ContinueWithoutRestore_ClosesAccepted_KeepsTheFiles_AndDoesNotRestoreAgain()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out List<string> restored);

        fixture.ClickButton("Continue without restore");
        fixture.WaitUntil(() => fixture.Dialog.Result != null, "the dialog to close");

        fixture.Dialog.Result.ShouldBe(true);
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeTrue();
        restored.Count.ShouldBe(1);
        File.Exists(fixture.ViewModel.CreatedGumProjectPath!).ShouldBeTrue();
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Back_ReturnsToAnEditableDialog_WithTheRefusalShown_AndOkOff()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out _);

        fixture.ClickButton("Back");
        fixture.Settle();

        fixture.Dialog.Result.ShouldBeNull();
        fixture.IsShown(RestoreFailedHeading).ShouldBeFalse("the panel is gone");
        fixture.Dialog.NameBox.IsEffectivelyEnabled.ShouldBeTrue("the form is editable again");
        fixture.IsShown(NewProjectUiText.RefusedHeading).ShouldBeTrue("the files just written now exist, so creating again would be refused");
        fixture.IsShownStartingWith("Project already exists").ShouldBeTrue();
        fixture.IsShown(NewProjectUiText.TargetNotEmptyHeading).ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();

        fixture.Dialog.TypeName("Game2");
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.RefusedHeading).ShouldBeFalse("a new name is free");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Back_ThenANewName_CreatesTheSecondProject_AndLeavesTheFirst()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out List<string> restored);
        fixture.ClickButton("Back");
        fixture.Dialog.TypeName("Game2");
        fixture.Settle();

        fixture.Dialog.ClickOk();

        restored.Count.ShouldBe(2);
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue("the first project stays");
        File.Exists(Path.Combine(fixture.GameFolder("Game2"), "Game2.csproj")).ShouldBeTrue();
        Path.GetFullPath(fixture.ViewModel.CreatedGumProjectPath!).ShouldBe(Path.Combine(fixture.GameFolder("Game2"), "Content", "GumProject", "GumProject.gumj"));
    }

    [AvaloniaFact]
    public void EscapeOnTheRestoreFailurePanel_ClosesTheDialogAsCanceled_AndKeepsTheFiles()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        FailRestoreOnce(fixture, out _);

        fixture.Window.Press(Key.Escape, PhysicalKey.Escape);

        fixture.Dialog.Result.ShouldBe(false);
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeFalse("a cancelled dialog does not hand the project to the tool");
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue("the files that were written stay");
    }

    [AvaloniaTheory]
    [InlineData("cancelButton")]
    [InlineData("escape")]
    public void CancelWhileTheRestoreRuns_StopsIt_KeepsTheDialogOpen_AndOffersTheThreeWaysForward(string how)
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        BlockRestore(fixture);
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsBusy && fixture.ViewModel.ProgressText.Contains("restore"), "the restore to start");
        fixture.IsShown("Running dotnet restore...").ShouldBeTrue();

        if (how == "cancelButton")
        {
            fixture.Window.Click(fixture.Dialog.CancelButton);
        }
        else
        {
            fixture.Window.Press(Key.Escape, PhysicalKey.Escape);
        }
        fixture.WaitUntil(() => !fixture.ViewModel.IsBusy, "the restore to stop");

        fixture.Dialog.Result.ShouldBeNull("Cancel while busy leaves the dialog open");
        fixture.IsShown("Restore was canceled.").ShouldBeTrue();
        fixture.ButtonLabeled("Retry").ShouldNotBeNull();
        fixture.ButtonLabeled("Continue without restore").ShouldNotBeNull();
        fixture.ButtonLabeled("Back").ShouldNotBeNull();

        List<string> restored = fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.ClickButton("Retry");
        fixture.WaitUntil(() => fixture.Dialog.Result != null, "the dialog to close");
        fixture.Dialog.Result.ShouldBe(true);
        restored.Count.ShouldBe(1);
    }

    [AvaloniaTheory]
    [InlineData("cancelButton")]
    [InlineData("escape")]
    public void CancelWhileTheFilesAreWritten_LeavesAnEditableDialogOpen_ThatCanCreateAfterwards(string how)
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        BlockCreation(fixture);
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsBusy, "the busy state");
        fixture.IsShown("Creating files...").ShouldBeTrue();
        fixture.Dialog.NameBox.IsEffectivelyEnabled.ShouldBeFalse("inputs are locked while busy");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();

        if (how == "cancelButton")
        {
            fixture.Window.Click(fixture.Dialog.CancelButton);
        }
        else
        {
            fixture.Window.Press(Key.Escape, PhysicalKey.Escape);
        }
        fixture.WaitUntil(() => !fixture.ViewModel.IsBusy, "the work to stop");

        fixture.Dialog.Result.ShouldBeNull();
        fixture.ViewModel.HasError.ShouldBeFalse("a cancel is not an error");
        fixture.Dialog.NameBox.IsEffectivelyEnabled.ShouldBeTrue();
        fixture.Settle();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
        Directory.Exists(fixture.GameFolder("Game")).ShouldBeFalse("nothing was written");

        fixture.Service.CreateFiles = null;
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Dialog.ClickOk();
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ClickingOkAgainOrPressingEnterWhileBusy_DoesNotStartASecondCreation()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        int started = 0;
        fixture.Service.CreateFiles = async (_, token) =>
        {
            Interlocked.Increment(ref started);
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
            }
            return new NewProjectFilesResult { Canceled = true };
        };
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsBusy, "the busy state");

        fixture.Window.Click(fixture.Dialog.OkButton);
        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);
        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);

        started.ShouldBe(1);
        fixture.Dialog.Result.ShouldBeNull();
        fixture.Window.Press(Key.Escape, PhysicalKey.Escape);
        fixture.WaitUntil(() => !fixture.ViewModel.IsBusy, "the work to stop");
    }

    [AvaloniaFact]
    public void ACreationThatThrows_LeavesTheDialogOpenWithTheError_AndTheErrorClearsWhenAnInputChanges()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.Service.CreateFiles = (_, _) => throw new InvalidOperationException("the disk blew up");
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.HasError, "the error");

        fixture.Dialog.Result.ShouldBeNull();
        fixture.IsShown("the disk blew up").ShouldBeTrue();
        fixture.ViewModel.IsBusy.ShouldBeFalse();
        fixture.Dialog.NameBox.IsEffectivelyEnabled.ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue("the user may try again");

        fixture.Dialog.TypeName("Game2");

        fixture.Settle();
        fixture.IsShown("the disk blew up").ShouldBeFalse("the error described the old inputs");
        fixture.ViewModel.HasError.ShouldBeFalse();
    }

    [AvaloniaTheory]
    [InlineData("platform")]
    [InlineData("checkbox")]
    [InlineData("location")]
    public void AFileCreationError_ClearsOnAnyInputChange(string input)
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.Service.CreateFiles = (_, _) => Task.FromResult(new NewProjectFilesResult { Success = false, ErrorMessage = "The folder is read-only." });
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.HasError, "the error");
        fixture.Settle();
        fixture.IsShown("The folder is read-only.").ShouldBeTrue();

        switch (input)
        {
            case "platform":
                fixture.Dialog.ChoosePlatform("raylib");
                break;
            case "checkbox":
                fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, false);
                break;
            default:
                fixture.Dialog.TypeLocation(fixture.MakeFolder("Else"));
                break;
        }

        fixture.Settle();
        fixture.IsShown("The folder is read-only.").ShouldBeFalse();
    }

    [AvaloniaFact]
    public void AfterAFileCreationError_FixingTheCauseAndPressingOkAgain_Succeeds()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Service.CreateFiles = (_, _) => Task.FromResult(new NewProjectFilesResult { Success = false, ErrorMessage = "The disk is full." });
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.HasError, "the error");

        fixture.Service.CreateFiles = null;
        fixture.Dialog.ClickOk();

        fixture.Dialog.Result.ShouldBe(true);
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ARestoreThatThrows_ShowsItsMessageOnThePanel()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.Service.Restore = (_, _, _) => throw new InvalidOperationException("restore crashed");
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the restore failure");

        fixture.IsShown("restore crashed").ShouldBeTrue();
        fixture.ButtonLabeled("Retry").ShouldNotBeNull();
    }

    [AvaloniaTheory]
    [InlineData(RestoreStatus.ProcessNotFound, "dotnet is not on the PATH", "", "dotnet is not on the PATH")]
    [InlineData(RestoreStatus.Failed, "", "", "dotnet restore failed.")]
    [InlineData(RestoreStatus.Failed, "ignored", "error NU1301: offline", "error NU1301: offline")]
    public void TheRestorePanel_ShowsTheMostUsefulTextTheRestoreGave(RestoreStatus status, string errorMessage, string tail, string expected)
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        fixture.StandInRestore(new RestoreResult { Status = status, ExitCode = 1, ErrorMessage = errorMessage, OutputTail = tail });
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the restore failure");

        fixture.IsShown(expected).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void WhileRestoring_TheDialogShowsTheLastLineTheRestorePrinted()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        TaskCompletionSource<RestoreResult> finish = new TaskCompletionSource<RestoreResult>();
        fixture.Service.Restore = (_, onOutputLine, _) =>
        {
            onOutputLine?.Invoke("Restoring packages for Game.csproj");
            return finish.Task;
        };
        StartCreation(fixture);
        fixture.WaitUntil(() => fixture.IsShown("Restoring packages for Game.csproj"), "the output line");

        fixture.IsShown("Running dotnet restore...").ShouldBeTrue();
        fixture.Dialog.CancelButton.IsEffectivelyEnabled.ShouldBeTrue();
        finish.SetResult(NewProjectUiFixture.RestoreOk());
        fixture.WaitUntil(() => fixture.Dialog.Result != null, "the dialog to close");
    }

    [AvaloniaFact]
    public void WithRestoreUnticked_TheDialogCreatesAndClosesWithoutRunningTheRestore()
    {
        using NewProjectUiFixture fixture = StartCodeProject();
        List<string> restored = fixture.StandInRestore(NewProjectUiFixture.RestoreFailed());
        fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, false);

        fixture.Dialog.ClickOk();

        restored.ShouldBeEmpty();
        fixture.Dialog.Result.ShouldBe(true);
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue();
    }
}
