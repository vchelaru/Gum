using System;
using System.Collections.Generic;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The rows of the Application > General page of the Settings window over
/// <see cref="GeneralSettingsViewModel"/>, built with <see cref="SettingsRows"/>. Each row reads and writes the
/// same view model property the page always did, so every setting is saved as before.
/// </summary>
public sealed class GeneralRows : IRowsSettingsPage
{
    private readonly GeneralSettingsViewModel _viewModel;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public GeneralRows(GeneralSettingsViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>The page never needs a rebuild: its rows are the only thing that changes its settings.</summary>
    public event Action? Reloaded
    {
        add { }
        remove { }
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        GeneralSettingsViewModel vm = _viewModel;

        InstanceMember folder = SettingsRows.ReadOnly("SettingsFolder", "Settings folder",
            "Where Gum keeps the settings of this machine, including the recent project list.",
            () => FileManager.UserApplicationDataForThisApplication);
        folder.DetailAction = new DetailTextLink("Open folder", () => vm.OpenSettingsFolderCommand.Execute(null));

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Saving", new[]
            {
                SettingsRows.Toggle(nameof(vm.AutoSave), "Auto save",
                    "Save the project whenever something changes.",
                    () => vm.AutoSave, value => vm.AutoSave = value, toggleText: "Save automatically"),
            }),
            SettingsRows.Section("Project panel", new[]
            {
                SettingsRows.Toggle(nameof(vm.UseStandardsPalette), "Standards palette",
                    "Show the standard types as a palette at the bottom of the Project panel instead of a Standard folder.",
                    () => vm.UseStandardsPalette, value => vm.UseStandardsPalette = value, toggleText: "Show as a palette"),
            }),
            SettingsRows.Section("Settings files", new[] { folder }),
        };
        SettingsRows.Finish(sections);
        return sections;
    }
}
