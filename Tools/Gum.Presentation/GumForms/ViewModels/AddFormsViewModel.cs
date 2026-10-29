using System;
using GumFormsPlugin.Services;
using Gum.Services.Dialogs;

namespace GumFormsPlugin.ViewModels;

public class AddFormsViewModel : DialogViewModel
{
    #region Fields/Properties

    private readonly IFormsThemeImporter _themeImporter;
    private readonly IDialogService _dialogService;

    public ThemeSelectionViewModel ThemeSelection { get; }

    /// <summary>The dialog's title.</summary>
    public string Title => "Add Forms";

    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    public AddFormsViewModel(ThemeSelectionViewModel themeSelection, IFormsThemeImporter themeImporter, IDialogService dialogService)
    {
        _dialogService = dialogService;
        ThemeSelection = themeSelection;
        _themeImporter = themeImporter;
    }

    public override async void OnAffirmative()
    {
        try
        {
            await _themeImporter.ImportThemeAsync(ThemeSelection.GetSelectedThemeOrDefault(), IsIncludeDemoScreenGum);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // async void: an escaping exception is unobserved. The importer reports its own failures;
            // this is for one it lets through.
            _dialogService.ShowMessage("Add Forms failed: " + exception.Message);
        }

        base.OnAffirmative();
    }
}
