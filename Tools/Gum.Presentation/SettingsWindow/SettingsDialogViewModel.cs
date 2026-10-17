using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace Gum.SettingsWindow;

/// <summary>One row of the Settings window's navigation list: a section header, or a selectable page.</summary>
public class SettingsNavigationEntry
{
    /// <summary>Creates an entry; <paramref name="page"/> is null for a section header.</summary>
    public SettingsNavigationEntry(string title, SettingsPage? page)
    {
        Title = title;
        Page = page;
    }

    /// <summary>The text shown.</summary>
    public string Title { get; }

    /// <summary>The page this entry selects, or null for a section header.</summary>
    public SettingsPage? Page { get; }

    /// <summary>True for a section header, which cannot be selected.</summary>
    public bool IsHeader => Page == null;
}

/// <summary>
/// The Settings window: a navigation list of pages in an Application section and, while a project is
/// loaded, a Project section, with the selected page's view beside it. Changes apply as they are made,
/// so the window only has a Close button.
/// </summary>
public class SettingsDialogViewModel : DialogViewModel
{
    private const int GeneralOrder = 0;
    private const int AppearanceOrder = 1;
    private const int PluginsOrder = 3;

    private readonly List<SettingsPage> _allPages;
    private readonly Dictionary<SettingsSection, SettingsNavigationEntry> _headers;
    private readonly Dictionary<string, SettingsNavigationEntry> _entriesByPageId;
    private SettingsNavigationEntry? _selectedEntry;
    private string _searchText;
    private readonly string _projectFileName;

    /// <summary>Creates the window's view model; the pages reflect the registry and project as they are now.</summary>
    public SettingsDialogViewModel(
        ISettingsPageRegistry registry,
        IProjectState projectState,
        GeneralSettingsViewModel general,
        ThemingDialogViewModel theming,
        PluginsDialogViewModel plugins)
    {
        Title = "Settings";
        AffirmativeText = "Close";
        NegativeText = null;
        _searchText = "";

        bool hasProject = projectState.GumProjectSave != null;
        _projectFileName = System.IO.Path.GetFileName(projectState.GumProjectSave?.FullFileName ?? "");
        _allPages = new List<SettingsPage>
        {
            new SettingsPage(SettingsPageIds.General, "General", SettingsSection.Application, general, GeneralOrder)
            {
                Keywords = new[] { "auto save", "autosave", "standards palette", "settings folder" },
            },
            new SettingsPage(SettingsPageIds.Appearance, "Appearance", SettingsSection.Application, theming, AppearanceOrder)
            {
                Keywords = new[] { "theme", "dark", "light", "accent", "color", "checker", "guide", "outline" },
            },
            new SettingsPage(SettingsPageIds.Plugins, "Plugins", SettingsSection.Application, plugins, PluginsOrder)
            {
                Keywords = new[] { "enable", "disable", "folder scan" },
            },
        };
        _allPages.AddRange(registry.Pages.Where(page => hasProject || page.Section == SettingsSection.Application));
        _allPages = _allPages
            .OrderBy(page => page.Section)
            .ThenBy(page => page.Order)
            .ThenBy(page => page.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _headers = new Dictionary<SettingsSection, SettingsNavigationEntry>
        {
            [SettingsSection.Application] = new SettingsNavigationEntry("Application", null),
            [SettingsSection.Project] = new SettingsNavigationEntry("Project", null),
        };
        _entriesByPageId = _allPages.ToDictionary(page => page.Id, page => new SettingsNavigationEntry(page.Title, page));

        NavigationEntries = new ObservableCollection<SettingsNavigationEntry>();
        RebuildNavigation();
        SelectEntry(NavigationEntries.First(entry => !entry.IsHeader));
    }

    /// <summary>The window title.</summary>
    public string Title { get => Get<string>(); set => Set(value); }

    /// <summary>The section headers and pages the search currently shows, in order.</summary>
    public ObservableCollection<SettingsNavigationEntry> NavigationEntries { get; }

    /// <summary>The page shown on the right, or null before one is chosen.</summary>
    public SettingsPage? SelectedPage => _selectedEntry?.Page;

    /// <summary>Where the selected page's settings live: this machine, or the project file.</summary>
    public string SelectedScopeText => SelectedPage?.Section switch
    {
        SettingsSection.Application => "Applies to this machine, whatever project is open.",
        SettingsSection.Project => string.IsNullOrEmpty(_projectFileName) ? "Saved in the project." : $"Saved in {_projectFileName}.",
        _ => "",
    };

    /// <summary>
    /// The selected row of <see cref="NavigationEntries"/>. A header or null is ignored, so the
    /// selection is always a page.
    /// </summary>
    public SettingsNavigationEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (value?.Page == null)
            {
                // Tell the list to put the real selection back.
                NotifyPropertyChanged();
                return;
            }
            SelectEntry(value);
        }
    }

    /// <summary>Text the page list is filtered by: a page matches on its title or keywords.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (value == _searchText)
            {
                return;
            }
            _searchText = value;
            NotifyPropertyChanged();
            RebuildNavigation();
        }
    }

    /// <summary>Shows the page with <paramref name="pageId"/>; an unknown id, or a page that is not listed, changes nothing.</summary>
    public void SelectPage(string pageId)
    {
        if (_entriesByPageId.TryGetValue(pageId, out SettingsNavigationEntry? entry))
        {
            SearchText = "";
            SelectEntry(entry);
        }
    }

    private void SelectEntry(SettingsNavigationEntry entry)
    {
        if (entry == _selectedEntry)
        {
            return;
        }
        _selectedEntry = entry;
        NotifyPropertyChanged(nameof(SelectedEntry));
        NotifyPropertyChanged(nameof(SelectedPage));
        NotifyPropertyChanged(nameof(SelectedScopeText));
        entry.Page?.Activated?.Invoke();
    }

    private void RebuildNavigation()
    {
        string search = _searchText.Trim();
        List<SettingsPage> visible = _allPages
            .Where(page => search.Length == 0 || Matches(page, search))
            .ToList();

        NavigationEntries.Clear();
        foreach (SettingsSection section in Enum.GetValues<SettingsSection>())
        {
            List<SettingsPage> inSection = visible.Where(page => page.Section == section).ToList();
            if (inSection.Count == 0)
            {
                continue;
            }
            NavigationEntries.Add(_headers[section]);
            foreach (SettingsPage page in inSection)
            {
                NavigationEntries.Add(_entriesByPageId[page.Id]);
            }
        }

        if (_selectedEntry?.Page != null && !visible.Contains(_selectedEntry.Page))
        {
            if (visible.Count > 0)
            {
                SelectEntry(_entriesByPageId[visible[0].Id]);
            }
        }
        else
        {
            // Clearing the list drops the list's selection; push the real one back.
            NotifyPropertyChanged(nameof(SelectedEntry));
        }
    }

    private static bool Matches(SettingsPage page, string search) =>
        page.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
        || page.Keywords.Any(keyword => keyword.Contains(search, StringComparison.OrdinalIgnoreCase));
}
