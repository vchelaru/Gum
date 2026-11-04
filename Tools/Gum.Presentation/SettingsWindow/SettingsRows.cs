using System;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// Builds the rows and sections of a Settings page in the design language of
/// <c>Direction/settings-design</c>: one setting per row, one label column, a one-line hint under every
/// row, and the standard row kinds. A page composes its sections from these instead of laying out its
/// own controls; <see cref="Finish"/> applies the shared row shape and <see cref="SearchTerms"/> reads
/// the label and hint of every row, which is what the window's search matches.
/// </summary>
public static class SettingsRows
{
    /// <summary>
    /// Width of the label column: wide enough that every label stays on one line. Set per member, so no
    /// other property grid changes.
    /// </summary>
    public const double LabelColumnWidth = 202;

    /// <summary>A checkbox row; <paramref name="toggleText"/> is the text beside the box.</summary>
    public static InstanceMember Toggle(string name, string label, string hint, Func<bool> get, Action<bool> set, string? toggleText = null)
    {
        InstanceMember member = Create(name, label, hint, typeof(bool), () => get(), value => set(Convert.ToBoolean(value)));
        member.ToggleText = toggleText;
        return member;
    }

    /// <summary>A single-line text row.</summary>
    public static InstanceMember Text(string name, string label, string hint, Func<string?> get, Action<string> set) =>
        Create(name, label, hint, typeof(string), () => get(), value => set((string?)value ?? string.Empty));

    /// <summary>A whole-number row. An entry that is not a number is ignored.</summary>
    public static InstanceMember Number(string name, string label, string hint, Func<int> get, Action<int> set) =>
        Create(name, label, hint, typeof(int), () => get(), value =>
        {
            if (int.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), out int number))
            {
                set(number);
            }
        });

    /// <summary>A drop-down row over <paramref name="options"/>.</summary>
    public static InstanceMember Choice(string name, string label, string hint, Type valueType, IEnumerable<object> options, Func<object?> get, Action<object?> set)
    {
        InstanceMember member = Create(name, label, hint, valueType, get, set);
        member.CustomOptions = options.ToList();
        return member;
    }

    /// <summary>A path row: a text field with browse and reveal buttons, for a file or (with <paramref name="isFolder"/>) a folder.</summary>
    public static InstanceMember Path(string name, string label, string hint, Func<string?> get, Action<string> set,
        bool isFolder = false, Func<string?>? revealRelativeTo = null)
    {
        InstanceMember member = Create(name, label, hint, typeof(string), () => get(), value => set((string?)value ?? string.Empty));
        member.PreferredDisplayer = typeof(StandardDisplayers.FileSelection);
        if (isFolder)
        {
            member.PropertiesToSetOnDisplayer["IsFolderDialog"] = true;
        }
        if (revealRelativeTo != null)
        {
            // The folder the value is relative to, when that is not the project folder; read on each reveal.
            member.PropertiesToSetOnDisplayer["RevealRelativeTo"] = revealRelativeTo;
        }
        return member;
    }

    /// <summary>Indents <paramref name="row"/> under the row above it, for the fields a toggle reveals.</summary>
    public static InstanceMember Nested(InstanceMember row)
    {
        row.IndentLevel = 1;
        return row;
    }

    /// <summary>A collapsible section bar over <paramref name="rows"/>.</summary>
    public static MemberCategory Section(string name, IEnumerable<InstanceMember> rows, bool isExpanded = true)
    {
        MemberCategory section = new MemberCategory(name) { IsExpanded = isExpanded };
        foreach (InstanceMember row in rows)
        {
            section.Members.Add(row);
        }
        return section;
    }

    /// <summary>
    /// Gives every row of <paramref name="sections"/> the shared row shape: the label column and the
    /// spacious layout that puts the hint under the field.
    /// </summary>
    public static void Finish(IEnumerable<MemberCategory> sections)
    {
        foreach (InstanceMember member in sections.SelectMany(section => section.Members))
        {
            member.FirstGridLength = LabelColumnWidth;
            member.IsSpaciousRow = true;
        }
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public static IEnumerable<string> SearchTerms(IEnumerable<MemberCategory> sections) =>
        sections.SelectMany(section => section.Members)
            .SelectMany(member => new[] { member.DisplayName, member.DetailText ?? string.Empty })
            .Where(term => term.Length > 0);

    /// <summary>
    /// Marks every row whose label or hint contains <paramref name="search"/> (ignoring case) and clears the
    /// mark on the rest; a blank search clears all. A section holding a match is expanded, so a match is
    /// never hidden in a collapsed section. Nothing is removed. Returns the number of rows marked.
    /// </summary>
    public static int MarkMatches(IEnumerable<MemberCategory> sections, string search)
    {
        string text = search.Trim();
        int count = 0;
        foreach (MemberCategory section in sections)
        {
            bool sectionHasMatch = false;
            foreach (InstanceMember member in section.Members)
            {
                bool isMatch = text.Length > 0
                    && (member.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase)
                        || (member.DetailText ?? string.Empty).Contains(text, StringComparison.OrdinalIgnoreCase));
                member.IsSearchMatch = isMatch;
                sectionHasMatch |= isMatch;
                if (isMatch)
                {
                    count++;
                }
            }
            if (sectionHasMatch && !section.IsExpanded)
            {
                section.IsExpanded = true;
            }
        }
        return count;
    }

    private static InstanceMember Create(string name, string label, string hint, Type type, Func<object?> get, Action<object?> set)
    {
        InstanceMember member = new InstanceMember(name, instance: null);
        member.DisplayName = label;
        member.DetailText = hint;
        member.CustomGetEvent += _ => get();
        member.CustomSetPropertyEvent += (_, args) => set(args.Value);
        member.CustomGetTypeEvent += _ => type;
        return member;
    }
}
