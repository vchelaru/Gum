using System;
using System.Collections.Generic;
using System.Linq;
using Gum.SettingsWindow;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// The rows of the Project > Localization page of the Settings window over the shared
/// <see cref="ProjectPropertiesViewModel"/>, built with <see cref="SettingsRows"/>. Each row reads and writes the
/// same view model property the page always did, so the saved data is unchanged. The files are one compact list
/// with Add; the Language choice exists only while the loaded files define languages.
/// </summary>
public sealed class LocalizationRows : IRowsSettingsPage
{
    private readonly ProjectPropertiesViewModel _viewModel;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public LocalizationRows(ProjectPropertiesViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <inheritdoc/>
    public event Action? Reloaded
    {
        add => _viewModel.Reloaded += value;
        remove => _viewModel.Reloaded -= value;
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        ProjectPropertiesViewModel vm = _viewModel;

        List<InstanceMember> languageRows = new List<InstanceMember>();
        IReadOnlyList<string> languages = vm.AvailableLanguages;
        if (languages.Count > 0)
        {
            languageRows.Add(SettingsRows.Choice(nameof(vm.LanguageName), "Language",
                "The language Gum shows text in. The loaded files define the choices.",
                typeof(string), languages.Cast<object>(), () => vm.LanguageName, value => vm.LanguageName = (string?)value ?? string.Empty));
        }
        languageRows.Add(SettingsRows.Toggle(nameof(vm.ShowLocalization), "Show Localization",
            "Off shows each text's string ID instead of its translation.",
            () => vm.ShowLocalization, value => vm.ShowLocalization = value, toggleText: "Show translations in Gum"));

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Files", new[]
            {
                SettingsFileListRow.Create(nameof(vm.LocalizationFiles), "Localization Files",
                    "One .csv with a column per language, or several .resx files.",
                    "Localization Files|*.csv;*.resx|All Files|*.*",
                    () => vm.LocalizationFiles, value => vm.LocalizationFiles = value),
            }),
            SettingsRows.Section("Language", languageRows),
        };
        SettingsRows.Finish(sections);
        return sections;
    }
}
