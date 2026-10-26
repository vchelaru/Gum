using System;
using GumFormsPlugin.Services;
using Gum.Services.Dialogs;

namespace GumFormsPlugin.ViewModels;

public class AddFormsViewModel : DialogViewModel
{
    #region Fields/Properties

    private readonly IFormsThemeImporter _themeImporter;

    public ThemeSelectionViewModel ThemeSelection { get; }

    /// <summary>The dialog's title.</summary>
    public string Title => "Add Forms";

    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    public AddFormsViewModel(ThemeSelectionViewModel themeSelection, IFormsThemeImporter themeImporter)
    {
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
            // async void: an escaping exception is unobserved. The importer reports its own failures
            // to the user; this keeps the dialog closing if it ever lets one through.
            System.Diagnostics.Trace.TraceError("Add Forms failed: " + exception);
        }

        base.OnAffirmative();
    }
}
