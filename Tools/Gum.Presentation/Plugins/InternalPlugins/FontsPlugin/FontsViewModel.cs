using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Gum.DataTypes;
using Gum.Mvvm;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Plugins.FontsPlugin;

/// <summary>
/// The Fonts tab: the character ranges baked per font (<see cref="Entries"/>, the project-wide default
/// row first) and the font generation settings, filled from and written back to the loaded project.
/// </summary>
public partial class FontsViewModel : ViewModel
{
    private GumProjectSave? _project;

    /// <summary>Creates a view model with no project; <see cref="SetFrom"/> fills it.</summary>
    public FontsViewModel()
    {
        Entries = new ObservableCollection<FontRangeEntryViewModel>();
        Entries.CollectionChanged += (_, _) => RaiseEntriesChanged();
    }

    /// <summary>
    /// The ranges table: the default row, then one row per font. Any add, remove or edit raises
    /// <c>PropertyChanged</c> for this property (unless the view model is being filled from the project).
    /// </summary>
    public ObservableCollection<FontRangeEntryViewModel> Entries { get; }

    /// <summary>Whether the project-wide ranges come from the project's .gumfcs file instead of being typed.</summary>
    public bool UseFontCharacterFile
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>True while the default row's ranges come from the font character file. View state, not a project setting.</summary>
    [DependsOn(nameof(UseFontCharacterFile))]
    public bool IsFontRangesReadOnly => UseFontCharacterFile;

    /// <summary>Pixels between glyphs in a baked atlas, horizontally.</summary>
    public int FontSpacingHorizontal
    {
        get => Get<int>();
        set => Set(value);
    }

    /// <summary>Pixels between glyphs in a baked atlas, vertically.</summary>
    public int FontSpacingVertical
    {
        get => Get<int>();
        set => Set(value);
    }

    /// <summary>Whether each font's atlas is shrunk to the smallest size that keeps it on one page.</summary>
    public bool AutoSizeFontOutputs
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>The program that bakes fonts.</summary>
    public FontGeneratorType FontGenerator
    {
        get => Get<FontGeneratorType>();
        set => Set(value);
    }

    /// <summary>The fonts the project's Texts use, offered when choosing a row's font. View state, not a project setting.</summary>
    public IReadOnlyList<string> AvailableFonts
    {
        get => Get<IReadOnlyList<string>>() ?? Array.Empty<string>();
        private set => Set(value);
    }

    /// <summary>True while <see cref="SetFrom"/> is filling this view model, so listeners can ignore the changes.</summary>
    public bool IsUpdatingFromModel { get; private set; }

    /// <summary>Raised by <see cref="NotifyReloaded"/> so a view that builds fields from the members can rebuild them.</summary>
    public event Action? Reloaded;

    /// <summary>Tells views the view model was refilled from the project. Called by the plugin.</summary>
    public void NotifyReloaded() => Reloaded?.Invoke();

    /// <summary>Fills this view model from <paramref name="project"/>.</summary>
    public void SetFrom(GumProjectSave project)
    {
        IsUpdatingFromModel = true;
        _project = project;

        foreach (FontRangeEntryViewModel entry in Entries)
        {
            entry.PropertyChanged -= HandleEntryPropertyChanged;
        }
        Entries.Clear();

        FontRangeEntryViewModel defaultEntry = new FontRangeEntryViewModel(isDefault: true) { Ranges = project.FontRanges };
        AddEntryInternal(defaultEntry);
        foreach (FontRangeEntry entry in project.FontRangeEntries.Where(entry => !string.IsNullOrWhiteSpace(entry.FontFamily)))
        {
            AddEntryInternal(new FontRangeEntryViewModel(isDefault: false) { FontFamily = entry.FontFamily!, Ranges = entry.Ranges });
        }

        UseFontCharacterFile = project.UseFontCharacterFile;
        FontSpacingHorizontal = project.FontSpacingHorizontal;
        FontSpacingVertical = project.FontSpacingVertical;
        AutoSizeFontOutputs = project.AutoSizeFontOutputs;
        FontGenerator = project.FontGenerator;

        RefreshReadOnly();
        ValidateFamilies();
        IsUpdatingFromModel = false;
    }

    /// <summary>Writes the table and settings back onto the project this view model was filled from.</summary>
    public void ApplyToModelObjects()
    {
        if (_project is null)
        {
            return;
        }

        List<FontRangeEntry> entries = new List<FontRangeEntry> { new FontRangeEntry { Ranges = DefaultEntry.Ranges } };
        foreach (FontRangeEntryViewModel row in Entries.Skip(1))
        {
            if (row.FamilyError == "")
            {
                entries.Add(new FontRangeEntry { FontFamily = row.FontFamily.Trim(), Ranges = row.Ranges });
            }
        }
        _project.FontRangeEntries = entries;

        _project.UseFontCharacterFile = UseFontCharacterFile;
        _project.FontSpacingHorizontal = FontSpacingHorizontal;
        _project.FontSpacingVertical = FontSpacingVertical;
        _project.AutoSizeFontOutputs = AutoSizeFontOutputs;
        _project.FontGenerator = FontGenerator;
    }

    /// <summary>The row for fonts with no row of their own.</summary>
    public FontRangeEntryViewModel DefaultEntry => Entries[0];

    /// <summary>Adds a row for a font, ready for its family to be chosen.</summary>
    [RelayCommand]
    public void AddEntry()
    {
        AddEntryInternal(new FontRangeEntryViewModel(isDefault: false));
        ValidateFamilies();
    }

    /// <summary>Removes <paramref name="entry"/>. The default row cannot be removed.</summary>
    [RelayCommand]
    public void RemoveEntry(FontRangeEntryViewModel? entry)
    {
        if (entry is null || entry.IsDefault || !Entries.Contains(entry))
        {
            return;
        }

        entry.PropertyChanged -= HandleEntryPropertyChanged;
        Entries.Remove(entry);
        ValidateFamilies();
    }

    /// <summary>
    /// Offers <paramref name="usedFontNames"/> when choosing a font, and warns on each row whose font is in
    /// <paramref name="unmatchedFamilies"/> (no Text uses it, so the row does nothing).
    /// </summary>
    public void UpdateFontUsage(IReadOnlyList<string> usedFontNames, IEnumerable<string> unmatchedFamilies)
    {
        AvailableFonts = usedFontNames;
        HashSet<string> unmatched = new HashSet<string>(unmatchedFamilies, StringComparer.Ordinal);
        foreach (FontRangeEntryViewModel entry in Entries.Skip(1))
        {
            entry.WarningText = unmatched.Contains(entry.FontFamily.Trim())
                ? $"No Text in this project uses {entry.FontFamily.Trim()}, so this row does nothing."
                : "";
        }
    }

    private void AddEntryInternal(FontRangeEntryViewModel entry)
    {
        entry.PropertyChanged += HandleEntryPropertyChanged;
        Entries.Add(entry);
    }

    private void HandleEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FontRangeEntryViewModel.FontFamily) or nameof(FontRangeEntryViewModel.Ranges))
        {
            if (e.PropertyName == nameof(FontRangeEntryViewModel.FontFamily))
            {
                ValidateFamilies();
            }
            RaiseEntriesChanged();
        }
    }

    private void RaiseEntriesChanged()
    {
        if (!IsUpdatingFromModel)
        {
            NotifyPropertyChanged(nameof(Entries));
        }
    }

    protected override void NotifyPropertyChanged(string? propertyName = null)
    {
        base.NotifyPropertyChanged(propertyName);
        if (propertyName == nameof(UseFontCharacterFile))
        {
            RefreshReadOnly();
        }
    }

    private void RefreshReadOnly()
    {
        if (Entries.Count > 0)
        {
            Entries[0].IsRangesReadOnly = UseFontCharacterFile;
        }
    }

    /// <summary>Flags the rows that cannot be saved: no font chosen, or a font an earlier row already names.</summary>
    private void ValidateFamilies()
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (FontRangeEntryViewModel row in Entries.Skip(1))
        {
            string family = row.FontFamily.Trim();
            if (family == "")
            {
                row.FamilyError = "Choose a font. A row with no font is not saved.";
            }
            else if (!seen.Add(NormalizeFamily(family)))
            {
                row.FamilyError = $"Another row already sets the ranges for {family}.";
            }
            else
            {
                row.FamilyError = "";
            }
        }
    }

    private static string NormalizeFamily(string family) => family.Replace('\\', '/');
}
