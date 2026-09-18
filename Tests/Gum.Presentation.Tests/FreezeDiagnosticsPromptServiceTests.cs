using Gum.Diagnostics;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class FreezeDiagnosticsPromptServiceTests
{
    private readonly Mock<IFreezeDiagnosticsInbox> _inbox;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileSystemRevealService> _revealService;
    private readonly FreezeDiagnosticsPromptService _service;
    private FreezeDiagnosticsPromptViewModel? _shownViewModel;

    public FreezeDiagnosticsPromptServiceTests()
    {
        _inbox = new Mock<IFreezeDiagnosticsInbox>();
        _dialogService = new Mock<IDialogService>();
        _revealService = new Mock<IFileSystemRevealService>();
        _inbox.SetupGet(x => x.DirectoryPath).Returns(@"C:\Users\someone\AppData\Roaming\Gum\FreezeDiagnostics");
        _inbox.Setup(x => x.GetUnreportedFiles()).Returns(new List<string> { "freeze-a.dmp", "freeze-a.txt" });
        _dialogService
            .Setup(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()))
            .Callback<FreezeDiagnosticsPromptViewModel>(viewModel => _shownViewModel = viewModel)
            .Returns(false);

        _service = new FreezeDiagnosticsPromptService(_inbox.Object, _dialogService.Object, _revealService.Object);
    }

    [Fact]
    public void PromptIfNeeded_CleanPreviousExit_DoesNotPrompt()
    {
        _service.PromptIfNeeded(previousSessionEndedDirty: false);

        _dialogService.Verify(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()), Times.Never);
        _inbox.Verify(x => x.MarkReported(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public void PromptIfNeeded_DirtyExitWithNoFiles_DoesNotPrompt()
    {
        _inbox.Setup(x => x.GetUnreportedFiles()).Returns(new List<string>());

        _service.PromptIfNeeded(previousSessionEndedDirty: true);

        _dialogService.Verify(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()), Times.Never);
    }

    [Fact]
    public void PromptIfNeeded_Suppressed_DoesNotPrompt()
    {
        _inbox.SetupGet(x => x.ArePromptsSuppressed).Returns(true);

        _service.PromptIfNeeded(previousSessionEndedDirty: true);

        _dialogService.Verify(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()), Times.Never);
    }

    [Fact]
    public void PromptIfNeeded_Dismissed_MarksFilesReportedWithoutOpeningFolder()
    {
        _service.PromptIfNeeded(previousSessionEndedDirty: true);

        _shownViewModel.ShouldNotBeNull();
        _shownViewModel.Message.ShouldContain("2 diagnostic files");
        _revealService.Verify(x => x.OpenFolder(It.IsAny<string>()), Times.Never);
        _inbox.Verify(x => x.MarkReported(new List<string> { "freeze-a.dmp", "freeze-a.txt" }), Times.Once);
        _inbox.Verify(x => x.SuppressPrompts(), Times.Never);
    }

    [Fact]
    public void PromptIfNeeded_Affirmed_OpensFolderAndMarksReported()
    {
        _dialogService
            .Setup(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()))
            .Returns(true);

        _service.PromptIfNeeded(previousSessionEndedDirty: true);

        _revealService.Verify(x => x.OpenFolder(@"C:\Users\someone\AppData\Roaming\Gum\FreezeDiagnostics"), Times.Once);
        _inbox.Verify(x => x.MarkReported(new List<string> { "freeze-a.dmp", "freeze-a.txt" }), Times.Once);
    }

    [Fact]
    public void PromptIfNeeded_DoNotAskAgainChecked_SuppressesFuturePrompts()
    {
        _dialogService
            .Setup(x => x.Show(It.IsAny<FreezeDiagnosticsPromptViewModel>()))
            .Callback<FreezeDiagnosticsPromptViewModel>(viewModel => viewModel.IsDoNotAskAgainChecked = true)
            .Returns(false);

        _service.PromptIfNeeded(previousSessionEndedDirty: true);

        _inbox.Verify(x => x.SuppressPrompts(), Times.Once);
        _inbox.Verify(x => x.MarkReported(It.IsAny<IEnumerable<string>>()), Times.Once);
    }
}
