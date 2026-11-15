using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.FontsPlugin;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices.FontGeneration;
using Gum.Services;
using WpfDataUi.DataTypes;

namespace Gum.SettingsWindow;

/// <summary>
/// The rows of the Project > Fonts page of the Settings window over <see cref="FontsViewModel"/>, built with
/// <see cref="SettingsRows"/>: a Character ranges section (the .gumfcs toggle, then the per-font ranges as one
/// compact list) and a Generation section with one row per setting. Each row reads and writes the same view
/// model property the page always did, so every setting is saved as before. Off Windows the generator row is
/// read-only, since bmfont.exe cannot run there and KernSmith is used regardless.
/// </summary>
public sealed class FontsRows : IRowsSettingsPage
{
    private readonly FontsViewModel _viewModel;
    private readonly IOperatingSystemInfo _operatingSystem;

    /// <summary>Creates the rows over <paramref name="viewModel"/> for the current OS unless one is given.</summary>
    public FontsRows(FontsViewModel viewModel, IOperatingSystemInfo? operatingSystem = null)
    {
        _viewModel = viewModel;
        _operatingSystem = operatingSystem ?? new OperatingSystemInfo();
    }

    /// <summary>Raised when the plugin refilled the view model from the project, so the rows are read again.</summary>
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
        FontsViewModel vm = _viewModel;

        InstanceMember characterFile = SettingsRows.Toggle(nameof(vm.UseFontCharacterFile), "Font character file",
            "Take the project-wide ranges from the text in the .gumfcs file instead of typing them below.",
            () => vm.UseFontCharacterFile, value => vm.UseFontCharacterFile = value,
            toggleText: "Use Font Character File (.gumfcs)");

        // The per-font list is one compact line per font (font, ranges, remove); its own row kind keeps it
        // a dense list instead of a tall row per font.
        InstanceMember ranges = SettingsRows.Custom("FontRanges", "Ranges per font",
            "The first line applies to every font without a line of its own.",
            typeof(GumDisplayers.FontRanges), () => vm);

        InstanceMember generator = SettingsRows.Choice(nameof(vm.FontGenerator), "Font Generator",
            "The program that bakes the font atlases.",
            typeof(FontGeneratorType), new object[] { FontGeneratorType.BmFont, FontGeneratorType.KernSmith },
            () => vm.FontGenerator, value => vm.FontGenerator = (FontGeneratorType)value!);
        if (FontGeneratorResolver.IsSubstituted(FontGeneratorType.BmFont, _operatingSystem.IsWindows))
        {
            generator.IsReadOnly = true;
            generator.DetailText = $"KernSmith is used on {_operatingSystem.DisplayName}";
        }

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Character ranges", new[] { characterFile, ranges }),
            SettingsRows.Section("Generation", new[]
            {
                generator,
                SettingsRows.Number(nameof(vm.FontSpacingHorizontal), "Font Spacing Horizontal",
                    "Pixels between glyphs in an atlas, left to right.",
                    () => vm.FontSpacingHorizontal, value => vm.FontSpacingHorizontal = value),
                SettingsRows.Number(nameof(vm.FontSpacingVertical), "Font Spacing Vertical",
                    "Pixels between glyphs in an atlas, top to bottom.",
                    () => vm.FontSpacingVertical, value => vm.FontSpacingVertical = value),
                SettingsRows.Toggle(nameof(vm.AutoSizeFontOutputs), "Auto-Size Font Outputs",
                    "Fewer PNGs, but font generation can be much slower",
                    () => vm.AutoSizeFontOutputs, value => vm.AutoSizeFontOutputs = value),
            }),
        };
        SettingsRows.Finish(sections);
        foreach (InstanceMember member in sections.SelectMany(section => section.Members))
        {
            member.SupportsMakeDefault = false;
        }
        return sections;
    }
}
