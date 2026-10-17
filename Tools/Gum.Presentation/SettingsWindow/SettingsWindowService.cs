using Gum.Services.Dialogs;

namespace Gum.SettingsWindow;

/// <summary>Opens the Settings window, optionally on a particular page.</summary>
public interface ISettingsWindowService
{
    /// <summary>Shows the window; <paramref name="pageId"/> (see <see cref="SettingsPageIds"/>) picks the page it opens on.</summary>
    void Show(string? pageId = null);
}

/// <inheritdoc/>
public class SettingsWindowService : ISettingsWindowService
{
    private readonly IDialogService _dialogService;

    /// <summary>Creates the service.</summary>
    public SettingsWindowService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <inheritdoc/>
    public void Show(string? pageId = null)
    {
        _dialogService.Show<SettingsDialogViewModel>(viewModel =>
        {
            if (pageId != null)
            {
                viewModel.SelectPage(pageId);
            }
        });
    }
}
