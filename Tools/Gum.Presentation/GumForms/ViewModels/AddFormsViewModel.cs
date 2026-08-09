using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using GumFormsPlugin.Services;
using Gum.Services.Dialogs;

namespace GumFormsPlugin.ViewModels;

public class AddFormsViewModel : DialogViewModel
{
    #region Fields/Properties

    private readonly IFormsFileService _formsFileService;
    private readonly IFormsThemeImporter _themeImporter;
    private readonly IProjectState _projectState;

    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    public IReadOnlyList<string> AvailableThemes
    {
        get => Get<IReadOnlyList<string>>() ?? Array.Empty<string>();
        private set => Set(value);
    }

    public string? SelectedTheme
    {
        get => Get<string?>();
        set
        {
            if (Set(value))
            {
                RefreshRequirementsDescription();
                NotifyPropertyChanged(nameof(HasRequirements));
            }
        }
    }

    public bool HasMultipleThemes => AvailableThemes.Count > 1;

    /// <summary>
    /// Bullet-formatted description of the project-level changes the import
    /// will apply for the currently-selected theme. Empty when the theme has
    /// no prerequisites that affect this project. Bound inline in the dialog
    /// so the user sees what's going to happen before clicking OK — no
    /// confirmation popup.
    /// </summary>
    public string RequirementsDescription
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    public bool HasRequirements => !string.IsNullOrEmpty(RequirementsDescription);

    #endregion

    public AddFormsViewModel(IFormsFileService formsFileService,
        IFormsThemeImporter themeImporter,
        IProjectState projectState)
    {
        _formsFileService = formsFileService;
        _themeImporter = themeImporter;
        _projectState = projectState;

        AvailableThemes = _formsFileService.GetAvailableThemes();
        SelectedTheme = AvailableThemes.FirstOrDefault(t =>
                            string.Equals(t, _formsFileService.DefaultThemeName, StringComparison.OrdinalIgnoreCase))
                        ?? AvailableThemes.FirstOrDefault();

        RefreshRequirementsDescription();
    }

    private void RefreshRequirementsDescription()
    {
        string? theme = SelectedTheme;
        if (string.IsNullOrEmpty(theme))
        {
            RequirementsDescription = string.Empty;
            return;
        }

        ThemeRequirements requirements =
            ThemeRequirements.LoadFromThemeDirectory(_formsFileService.GetThemeDirectory(theme));
        ThemeRequirementsDiff diff = requirements.Diff(_projectState.GumProjectSave);
        RequirementsDescription = diff.HasChanges
            ? string.Join(Environment.NewLine, diff.DescribeChanges().Select(c => "• " + c))
            : string.Empty;
    }

    public override void OnAffirmative()
    {
        _themeImporter.ImportTheme(SelectedTheme ?? _formsFileService.DefaultThemeName, IsIncludeDemoScreenGum);

        base.OnAffirmative();
    }
}
