using GumFormsPlugin.Services;
using Gum.Services.Dialogs;

namespace GumFormsPlugin.ViewModels;

public class AddFormsViewModel : DialogViewModel
{
    #region Fields/Properties

    private readonly IFormsThemeImporter _themeImporter;

    public ThemeSelectionViewModel ThemeSelection { get; }

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

    public override void OnAffirmative()
    {
        _themeImporter.ImportTheme(ThemeSelection.GetSelectedThemeOrDefault(), IsIncludeDemoScreenGum);

        base.OnAffirmative();
    }
}
