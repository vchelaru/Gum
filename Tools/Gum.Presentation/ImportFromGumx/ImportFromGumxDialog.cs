using System.Threading.Tasks;
using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;

namespace ImportFromGumxPlugin;

/// <summary>
/// Shows the Import from .gumx dialog on a source another feature already chose.
/// </summary>
public interface IImportFromGumxDialog
{
    /// <summary>
    /// Loads <paramref name="source"/>, shows the dialog, and returns true when the user imported something.
    /// </summary>
    Task<bool> ShowAsync(FixedImportSource source);
}

/// <inheritdoc/>
public class ImportFromGumxDialog : IImportFromGumxDialog
{
    private readonly IImportFromGumxLogic _logic;
    private readonly IDialogService _dialogService;

    public ImportFromGumxDialog(IImportFromGumxLogic logic, IDialogService dialogService)
    {
        _logic = logic;
        _dialogService = dialogService;
    }

    /// <inheritdoc/>
    public async Task<bool> ShowAsync(FixedImportSource source)
    {
        ImportFromGumxViewModel viewModel = _logic.CreateImportViewModel();
        await viewModel.UseFixedSourceAsync(source);
        _dialogService.Show(viewModel);
        return viewModel.WasImported;
    }
}
