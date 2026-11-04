using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Gum.SettingsWindow;
using WpfDataUi.DataTypes;

namespace Gum.Dialogs;

/// <summary>
/// The rows of the Application > Appearance page of the Settings window over
/// <see cref="ThemingDialogViewModel"/>, built with <see cref="SettingsRows"/>. Each row reads and writes
/// the same view model property the page always did, so the saved theme is unchanged. Each color has its
/// own reset, shown while the color is not the default.
/// </summary>
public sealed class AppearanceRows : IRowsSettingsPage
{
    private readonly ThemingDialogViewModel _viewModel;
    private List<InstanceMember> _members = new List<InstanceMember>();
    private Action? _reloaded;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public AppearanceRows(ThemingDialogViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <summary>
    /// Raised when the view should rebuild its rows. The page never needs a rebuild (a change only refreshes
    /// the rows), but the view's subscription is what tells the page it is on screen, so it follows the
    /// view model's changes only for that long.
    /// </summary>
    public event Action? Reloaded
    {
        add
        {
            if (_reloaded == null)
            {
                _viewModel.PropertyChanged += HandleViewModelChanged;
            }
            _reloaded += value;
        }
        remove
        {
            _reloaded -= value;
            if (_reloaded == null)
            {
                _viewModel.PropertyChanged -= HandleViewModelChanged;
            }
        }
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        ThemingDialogViewModel vm = _viewModel;

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Theme", new[]
            {
                SettingsRows.Choice(nameof(vm.Mode), "Theme mode",
                    "Light, dark, or whichever the operating system uses.",
                    typeof(ThemeMode), vm.ThemeModes.Cast<object>(),
                    () => vm.Mode ?? ThemeMode.System, value => vm.Mode = (ThemeMode?)value),
                ColorRow(nameof(vm.AccentColor), "Accent color",
                    "Highlights selection, focus and buttons across the tool.",
                    () => vm.AccentColor, color => vm.AccentColor = color, () => vm.HasExplicitAccentColor),
            }),
            SettingsRows.Section("Editor background", new[]
            {
                ColorRow(nameof(vm.CheckerAColor), "Editor background",
                    "Fills the Editor tab behind your elements, and is the first color of the checkerboard.",
                    () => vm.CheckerAColor, color => vm.CheckerAColor = color, () => vm.HasExplicitCheckerAColor),
                ColorRow(nameof(vm.CheckerBColor), "Checker color",
                    "The second color of the checkerboard, drawn when the Editor tab shows it.",
                    () => vm.CheckerBColor, color => vm.CheckerBColor = color, () => vm.HasExplicitCheckerBColor),
            }),
            SettingsRows.Section("Outlines and guides", new[]
            {
                ColorRow(nameof(vm.OutlineColor), "Outline color",
                    "Outlines drawn around containers and around the whole design area.",
                    () => vm.OutlineColor, color => vm.OutlineColor = color, () => vm.HasExplicitOutlineColor),
                ColorRow(nameof(vm.GuideLineColor), "Guide line color",
                    "Lines the Editor tab draws to show alignment and measurements.",
                    () => vm.GuideLineColor, color => vm.GuideLineColor = color, () => vm.HasExplicitGuideLineColor),
                ColorRow(nameof(vm.GuideTextColor), "Guide text color",
                    "Text on those guide lines, such as distances.",
                    () => vm.GuideTextColor, color => vm.GuideTextColor = color, () => vm.HasExplicitGuideTextColor),
            }),
        };
        SettingsRows.Finish(sections);
        _members = sections.SelectMany(section => section.Members).ToList();
        return sections;
    }

    private InstanceMember ColorRow(string name, string label, string hint, Func<Color?> get, Action<Color?> set, Func<bool> isCustom) =>
        SettingsRows.Color(name, label, hint, () => get() ?? Color.Empty, color => set(color),
            reset: () => _viewModel.ResetCommand.Execute(name), isCustom: isCustom);

    // A change to one setting can change what others show (the theme mode picks the editor colors), and
    // a color's reset button follows whether it is custom, so every row reads its value again.
    private void HandleViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (InstanceMember member in _members)
        {
            member.SimulateValueChanged();
        }
    }
}
