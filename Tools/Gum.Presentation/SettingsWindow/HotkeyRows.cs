using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The rows of the Application > Keyboard shortcuts page of the Settings window over
/// <see cref="HotkeyViewModel"/>: one compact line per command (its name and its keys as a chip), grouped by
/// area under section bars. The page lists the shortcuts and does not change them, so there is no hint and no
/// editor on a row.
/// </summary>
public sealed class HotkeyRows : IRowsSettingsPage
{
    private readonly HotkeyViewModel _viewModel;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public HotkeyRows(HotkeyViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>Never raised: the shortcuts are fixed for the life of the page.</summary>
    public event Action? Reloaded
    {
        add { }
        remove { }
    }

    /// <summary>The name and keys of every shortcut, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        List<MemberCategory> sections = new List<MemberCategory>();
        foreach (string group in HotkeyGroups.All)
        {
            List<InstanceMember> rows = _viewModel.Items
                .Where(item => item.Group == group)
                .Select(item => KeyChipRow(item))
                .ToList();
            if (rows.Count > 0)
            {
                sections.Add(SettingsRows.Section(group, rows));
            }
        }
        SettingsRows.Finish(sections);
        return sections;
    }

    private static InstanceMember KeyChipRow(HotkeyItemViewModel item)
    {
        string shortcut = item.Shortcut;
        InstanceMember row = SettingsRows.ReadOnly("Shortcut:" + item.Name, item.Name, null, () => shortcut, chip: shortcut);
        row.PreferredDisplayer = typeof(StandardDisplayers.KeyChip);
        return row;
    }
}
