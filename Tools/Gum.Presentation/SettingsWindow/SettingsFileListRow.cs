using System;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The "list with Add" row kind of a Settings page: the tool's file list editor (one compact line per file, with
/// Add, Remove, Up and Down) in a row of the shared design. Kept apart from <see cref="SettingsRows"/> so a page
/// needing it does not touch that shared file.
/// </summary>
public static class SettingsFileListRow
{
    /// <summary>
    /// A row over a list of file paths. <paramref name="filter"/> is the open dialog's filter
    /// (<c>"Name|*.ext|All Files|*.*"</c>).
    /// </summary>
    public static InstanceMember Create(string name, string label, string hint, string filter,
        Func<IReadOnlyList<string>> get, Action<List<string>> set)
    {
        InstanceMember member = new InstanceMember(name, instance: null);
        member.DisplayName = label;
        member.DetailText = hint;
        member.CustomGetEvent += _ => get().ToList();
        member.CustomSetPropertyEvent += (_, args) => set(((args.Value as IEnumerable<string>) ?? Array.Empty<string>()).ToList());
        member.CustomGetTypeEvent += _ => typeof(List<string>);
        member.PreferredDisplayer = typeof(StandardDisplayers.MultiFile);
        member.PropertiesToSetOnDisplayer["Filter"] = filter;
        return member;
    }
}
