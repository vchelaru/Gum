using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.HideShowTools;
using Gum.Settings;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The shell's tab host: the Avalonia counterpart of the WPF <c>MainPanelViewModel</c>. Keeps one
/// observable list per <see cref="TabLocation"/> in sync with the tabs' location and visibility, and
/// persists the column widths on teardown.
/// </summary>
public class AvaloniaTabManager : ViewModel, ITabManager, IToolsVisibility, IRecipient<ApplicationTeardownMessage>
{
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;
    private readonly ObservableCollection<AvaloniaPluginTab> _tabs;
    private readonly TabViewRegistry _tabViewRegistry;

    /// <summary>Creates the manager and restores the saved column widths.</summary>
    public AvaloniaTabManager(IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings, TabViewRegistry tabViewRegistry)
    {
        _layoutSettings = layoutSettings;
        _tabViewRegistry = tabViewRegistry;
        _tabs = new ObservableCollection<AvaloniaPluginTab>();
        Left = new ObservableCollection<AvaloniaPluginTab>();
        CenterTop = new ObservableCollection<AvaloniaPluginTab>();
        CenterBottom = new ObservableCollection<AvaloniaPluginTab>();
        RightTop = new ObservableCollection<AvaloniaPluginTab>();
        RightBottom = new ObservableCollection<AvaloniaPluginTab>();

        MainTabDimensions dimensions = layoutSettings.CurrentValue.MainTabDimensions;
        LeftColumnWidth = dimensions.LeftColumnWidth;
        CenterColumnWidth = dimensions.CenterColumnWidth;
        BottomRightHeight = dimensions.BottomRightHeight;
        IsToolsVisible = true;

        messenger.RegisterAll(this);
    }

    /// <summary>Every tab, regardless of location or visibility.</summary>
    public ReadOnlyObservableCollection<AvaloniaPluginTab> AllTabs => new ReadOnlyObservableCollection<AvaloniaPluginTab>(_tabs);

    /// <summary>Visible tabs docked at the left.</summary>
    public ObservableCollection<AvaloniaPluginTab> Left { get; }

    /// <summary>Visible tabs docked at the center top.</summary>
    public ObservableCollection<AvaloniaPluginTab> CenterTop { get; }

    /// <summary>Visible tabs docked at the center bottom.</summary>
    public ObservableCollection<AvaloniaPluginTab> CenterBottom { get; }

    /// <summary>Visible tabs docked at the right top.</summary>
    public ObservableCollection<AvaloniaPluginTab> RightTop { get; }

    /// <summary>Visible tabs docked at the right bottom.</summary>
    public ObservableCollection<AvaloniaPluginTab> RightBottom { get; }

    /// <summary>Width of the left column in device-independent units.</summary>
    public double LeftColumnWidth { get => Get<double>(); set => Set(value); }

    /// <summary>Width of the center column in device-independent units.</summary>
    public double CenterColumnWidth { get => Get<double>(); set => Set(value); }

    /// <summary>Height of the bottom-right panel in device-independent units.</summary>
    public double BottomRightHeight { get => Get<double>(); set => Set(value); }

    /// <inheritdoc/>
    public bool IsToolsVisible { get => Get<bool>(); set => Set(value); }

    /// <inheritdoc/>
    public IPluginTab AddControl(object element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
    {
        // A plugin in Gum.Presentation hands over a ViewModel; this head resolves it to its view (phase 40).
        object content = element;
        Control? header = null;
        if (element is not Control && _tabViewRegistry.CreateView(element) is { } view)
        {
            content = view;
            header = _tabViewRegistry.CreateHeader(element);
        }

        AvaloniaPluginTab tab = new AvaloniaPluginTab(content)
        {
            Title = tabTitle,
            Location = tabLocation,
            HeaderContent = header,
        };
        tab.PropertyChanged += OnTabPropertyChanged;
        _tabs.Add(tab);
        Refilter();
        TabAutoSelectLogic.SelectNewTabsWithNoExistingSelection(_tabs, new[] { tab });
        return tab;
    }

    /// <inheritdoc/>
    public void RemoveTab(IPluginTab tab)
    {
        AvaloniaPluginTab typed = (AvaloniaPluginTab)tab;
        typed.PropertyChanged -= OnTabPropertyChanged;
        _tabs.Remove(typed);
        Refilter();
    }

    /// <inheritdoc/>
    public void EnsureMinimumWidth()
    {
        const int minWidth = 20;
        LeftColumnWidth = Math.Max(LeftColumnWidth, minWidth);
        CenterColumnWidth = Math.Max(CenterColumnWidth, minWidth);
        BottomRightHeight = Math.Max(BottomRightHeight, minWidth);
    }

    /// <summary>
    /// Raised when a tab becomes selected, by the user or by a plugin setting
    /// <see cref="IPluginTab.IsSelected"/>; the view brings it to the front of its region.
    /// </summary>
    public event Action<AvaloniaPluginTab>? TabSelected;

    /// <summary>Marks <paramref name="tab"/> selected and every other tab in its location unselected.</summary>
    public void Select(AvaloniaPluginTab tab) => tab.IsSelected = true;

    void IRecipient<ApplicationTeardownMessage>.Receive(ApplicationTeardownMessage message)
    {
        message.OnTearDown(() => _layoutSettings.Update(l => l.MainTabDimensions = new MainTabDimensions
        {
            LeftColumnWidth = LeftColumnWidth,
            CenterColumnWidth = CenterColumnWidth,
            BottomRightHeight = BottomRightHeight,
        }));
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AvaloniaPluginTab.Location) or nameof(AvaloniaPluginTab.IsVisible))
        {
            Refilter();
        }
        else if (e.PropertyName == nameof(AvaloniaPluginTab.IsSelected) && sender is AvaloniaPluginTab { IsSelected: true } tab)
        {
            // One selected tab per region, as the WPF PluginTab's TabSelectedMessage enforces.
            foreach (AvaloniaPluginTab other in _tabs.Where(t => t.Location == tab.Location && t != tab))
            {
                other.IsSelected = false;
            }
            TabSelected?.Invoke(tab);
        }
    }

    private void Refilter()
    {
        Sync(Left, TabLocation.Left);
        Sync(CenterTop, TabLocation.CenterTop);
        Sync(CenterBottom, TabLocation.CenterBottom);
        Sync(RightTop, TabLocation.RightTop);
        Sync(RightBottom, TabLocation.RightBottom);
    }

    // The WPF head shows its bottom-right tabs in the order its plugins happen to load (the plugin
    // DLLs by file name, then the built-in ones). The same plugins load in another order here, so
    // the tabs are ranked by title instead; any other tab keeps its order of arrival after these.
    private static readonly string[] KnownTabOrder = { "Code", "Performance", "Texture Coordinates", "History", "Output", "Errors" };

    private static int Rank(AvaloniaPluginTab tab)
    {
        int index = Array.IndexOf(KnownTabOrder, tab.Title);
        return index < 0 ? int.MaxValue : index;
    }

    private void Sync(ObservableCollection<AvaloniaPluginTab> target, TabLocation location)
    {
        // OrderBy is stable, so tabs of the same rank keep their order of arrival.
        AvaloniaPluginTab[] wanted = _tabs.Where(t => TabDockingLogic.ShouldAppearInLocation(t, location)).OrderBy(Rank).ToArray();
        if (wanted.SequenceEqual(target))
        {
            return;
        }
        target.Clear();
        foreach (AvaloniaPluginTab tab in wanted)
        {
            target.Add(tab);
        }
    }
}
