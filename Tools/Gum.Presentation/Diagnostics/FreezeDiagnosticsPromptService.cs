using System.Collections.Generic;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Diagnostics;

/// <summary>
/// The standard dirty-shutdown prompt: a dump alone is not enough (the watchdog also fires on a
/// stall the app recovered from), and a dirty exit alone is not enough (a debugger stop leaves the
/// sentinel behind too). Both together mean the user most likely killed a frozen Gum and has a dump
/// worth sending. Whatever they answer, the files move to the Reported folder so the same freeze
/// never prompts twice.
/// </summary>
public class FreezeDiagnosticsPromptService : IFreezeDiagnosticsPromptService
{
    private readonly IFreezeDiagnosticsInbox _inbox;
    private readonly IDialogService _dialogService;
    private readonly IFileSystemRevealService _revealService;

    public FreezeDiagnosticsPromptService(IFreezeDiagnosticsInbox inbox, IDialogService dialogService, IFileSystemRevealService revealService)
    {
        _inbox = inbox;
        _dialogService = dialogService;
        _revealService = revealService;
    }

    /// <inheritdoc/>
    public void PromptIfNeeded(bool previousSessionEndedDirty)
    {
        if (!previousSessionEndedDirty || _inbox.ArePromptsSuppressed)
        {
            return;
        }

        IReadOnlyList<string> files = _inbox.GetUnreportedFiles();
        if (files.Count == 0)
        {
            return;
        }

        FreezeDiagnosticsPromptViewModel viewModel = new FreezeDiagnosticsPromptViewModel
        {
            Message =
                "Gum did not shut down cleanly last time, and its freeze watchdog captured " +
                $"{files.Count} diagnostic files that can help find the cause.\n\n" +
                "Please attach them to a GitHub issue at github.com/vchelaru/Gum/issues, or share " +
                "them on the FlatRedBall Discord. They are in:\n" + _inbox.DirectoryPath,
        };

        bool openFolder = _dialogService.Show(viewModel);

        if (openFolder)
        {
            _revealService.OpenFolder(_inbox.DirectoryPath);
        }

        _inbox.MarkReported(files);

        if (viewModel.IsDoNotAskAgainChecked)
        {
            _inbox.SuppressPrompts();
        }
    }
}
