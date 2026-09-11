using System.Collections.Generic;
using System.Linq;
using Gum.Services.Dialogs;
using WpfDataUi.Controls;

namespace Gum.Services;

/// <summary>
/// Opens the property grid's file pickers through the head's <see cref="IDialogService"/> and
/// reveals files through <see cref="IFileSystemRevealService"/>, so both heads show their native
/// dialogs and file manager. Assigned to <see cref="FilePickingLogic.FilePicker"/> at startup.
/// </summary>
public class DialogServiceFilePicker : IDataUiFilePicker
{
    private readonly IDialogService _dialogService;
    private readonly IFileSystemRevealService _revealService;

    public DialogServiceFilePicker(IDialogService dialogService, IFileSystemRevealService revealService)
    {
        _dialogService = dialogService;
        _revealService = revealService;
    }

    /// <inheritdoc/>
    public string? PickFile(string filter)
    {
        OpenFileDialogOptions options = string.IsNullOrEmpty(filter)
            ? new OpenFileDialogOptions()
            : new OpenFileDialogOptions { Filter = filter };

        List<string>? selected = _dialogService.OpenFile(options);
        return selected?.FirstOrDefault();
    }

    /// <inheritdoc/>
    public void RevealFile(string filePath) => _revealService.RevealFile(filePath);
}
