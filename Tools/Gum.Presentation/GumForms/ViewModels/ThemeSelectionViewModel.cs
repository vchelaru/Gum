using Gum.Mvvm;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using GumFormsPlugin.Services;

namespace GumFormsPlugin.ViewModels;

/// <summary>
/// Theme picker (available themes, selected theme, and the project-change requirements panel for
/// the current selection). Shared between the Add Forms dialog and the New Project dialog so both
/// present the same picking UI.
/// </summary>
public class ThemeSelectionViewModel : ViewModel
{
    private readonly IFormsFileService _formsFileService;
    private readonly IProjectState _projectState;

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
    /// Bullet-formatted description of the project-level changes the import will apply for the
    /// currently-selected theme. Empty when the theme has no prerequisites that affect this
    /// project. Bound inline in the dialog so the user sees what's going to happen before clicking
    /// OK — no confirmation popup.
    /// </summary>
    public string RequirementsDescription
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    public bool HasRequirements => !string.IsNullOrEmpty(RequirementsDescription);

    public ThemeSelectionViewModel(IFormsFileService formsFileService, IProjectState projectState)
    {
        _formsFileService = formsFileService;
        _projectState = projectState;

        AvailableThemes = _formsFileService.GetAvailableThemes();
        SelectedTheme = AvailableThemes.FirstOrDefault(t =>
                            string.Equals(t, _formsFileService.DefaultThemeName, StringComparison.OrdinalIgnoreCase))
                        ?? AvailableThemes.FirstOrDefault();

        RefreshRequirementsDescription();
    }

    /// <summary>
    /// The theme to import: <see cref="SelectedTheme"/>, falling back to
    /// <see cref="IFormsFileService.DefaultThemeName"/> when nothing is selected (no themes shipped).
    /// </summary>
    public string GetSelectedThemeOrDefault() => SelectedTheme ?? _formsFileService.DefaultThemeName;

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
}
