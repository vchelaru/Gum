using Avalonia.Headless.XUnit;
using Gum.Avalonia.Diagnostics;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// An unattended (<c>--exit-after</c>) run cancels every dialog instead of waiting on it, and flags an
/// OK-only message, which may report an error, so the run exits nonzero (#8451).
/// </summary>
public class UnattendedDialogServiceTests
{
    [AvaloniaFact]
    public void Show_CancelsTheDialogWithoutShowingIt_AndNamesIt()
    {
        StringWriter error = new StringWriter();
        UnattendedDialogService service = new UnattendedDialogService(TestAppBuilder.Services, error);

        bool accepted = service.Show<NewProjectDialogViewModel>(_ => { }, out NewProjectDialogViewModel viewModel);

        accepted.ShouldBeFalse();
        viewModel.ShouldNotBeNull();
        error.ToString().ShouldContain("NewProjectDialogViewModel");
        service.HasShownNotice.ShouldBeFalse("a cancelled question is not an error");
    }

    // Callers may read the view model after a cancel, as with the head's own dialog service.
    [AvaloniaFact]
    public void Show_RunsTheInitializer_BeforeCancelling()
    {
        UnattendedDialogService service = new UnattendedDialogService(TestAppBuilder.Services, new StringWriter());
        NewProjectDialogViewModel? initialized = null;

        service.Show<NewProjectDialogViewModel>(vm => initialized = vm, out NewProjectDialogViewModel viewModel);

        initialized.ShouldBeSameAs(viewModel);
    }

    [Fact]
    public void ShowMessage_Question_IsCancelled_WithoutCountingAsANotice()
    {
        StringWriter error = new StringWriter();
        UnattendedDialogService service = new UnattendedDialogService(TestAppBuilder.Services, error);

        MessageDialogResult result = service.ShowMessage("Save changes?", style: MessageDialogStyle.YesNo);

        result.ShouldBe(MessageDialogResult.Canceled);
        service.HasShownNotice.ShouldBeFalse();
        error.ToString().ShouldContain("Save changes?");
    }

    [Fact]
    public void ShowMessage_OkOnly_CountsAsANotice_AndWritesItsText()
    {
        StringWriter error = new StringWriter();
        UnattendedDialogService service = new UnattendedDialogService(TestAppBuilder.Services, error);

        service.ShowMessage("Errors loading Game.gumx", "Load failed");

        service.HasShownNotice.ShouldBeTrue();
        error.ToString().ShouldContain("Load failed: Errors loading Game.gumx");
    }
}
