using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
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

    // Tabs that stay in front only when the user puts them there: the Code tab generates code
    // whenever it has focus, which slows every selection while it is the default (#4694).
    private static readonly string[] DeferredDefaultTabs = { "Code" };

    // Deferred tabs in front only for lack of an alternative; the next other tab replaces them.
    private readonly HashSet<AvaloniaPluginTab> _defaultedDeferredTabs;

    /// <summary>Creates the manager and restores the saved column widths.</summary>
    public AvaloniaTabManager(IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings, TabViewRegistry tabViewRegistry)
    {
        _layoutSettings = layoutSettings;
        _tabViewRegistry = tabViewRegistry;
        _tabs = new ObservableCollection<AvaloniaPluginTab>();
        _defaultedDeferredTabs = new HashSet<AvaloniaPluginTab>();
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

    /// <summary>
    /// True while a region's list is being brought in line with the tabs. The view ignores the
    /// selection changes its tab control makes on its own meanwhile; the manager settles the
    /// selection once the list is in order.
    /// </summary>
    public bool IsSyncing { get; private set; }

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

    /// <summary>
    /// Marks <paramref name="tab"/> selected and every other tab in its location unselected, as the
    /// user's choice: a deferred default the user picks stays in front.
    /// </summary>
    public void Select(AvaloniaPluginTab tab)
    {
        _defaultedDeferredTabs.Remove(tab);
        tab.IsSelected = true;
    }

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
            // A plugin can show/hide a tab from inside that same tab's own GotFocus/IsSelected
            // handler (CodeOutputPlugin hides itself when there's nothing to display), which runs
            // synchronously inside the hosting TabControl's own SelectionChanged. Clearing/rebuilding
            // the ObservableCollection that IS that TabControl's ItemsSource while it is still
            // resolving its own selection change corrupts its internal selected-index bookkeeping
            // (ArgumentOutOfRangeException). Deferring to the next dispatcher cycle lets the current
            // selection change finish first.
            Dispatcher.UIThread.Post(Refilter);
            if (sender is AvaloniaPluginTab { IsVisible: false } hidden)
            {
                // A hidden tab is not in front; the region settles on another when it refilters.
                hidden.IsSelected = false;
            }
        }
        else if (e.PropertyName == nameof(AvaloniaPluginTab.IsSelected) && sender is AvaloniaPluginTab { IsSelected: true } tab)
        {
            _defaultedDeferredTabs.Remove(tab);
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
        if (!wanted.SequenceEqual(target))
        {
            // Changed in place rather than rebuilt: the region's tab control keeps its selection
            // through an insert or a removal elsewhere in its list, but a rebuild resets it to the
            // first tab (#4694).
            IsSyncing = true;
            try
            {
                for (int i = target.Count - 1; i >= 0; i--)
                {
                    if (!wanted.Contains(target[i]))
                    {
                        target.RemoveAt(i);
                    }
                }
                for (int i = 0; i < wanted.Length; i++)
                {
                    if (i < target.Count && target[i] == wanted[i])
                    {
                        continue;
                    }
                    int existing = target.IndexOf(wanted[i]);
                    if (existing >= 0)
                    {
                        // A retitled tab can change rank; move it rather than insert a second copy.
                        target.Move(existing, i);
                    }
                    else
                    {
                        target.Insert(i, wanted[i]);
                    }
                }
            }
            finally
            {
                IsSyncing = false;
            }
        }
        EnsureOneSelected(wanted);
    }

    // Keeps one of the region's visible tabs in front: any tab ahead of a deferred default, and a
    // deferred default that is only in front for lack of an alternative gives way to the first other tab.
    private void EnsureOneSelected(AvaloniaPluginTab[] visible)
    {
        if (visible.Length == 0)
        {
            return;
        }

        AvaloniaPluginTab? selected = visible.FirstOrDefault(t => t.IsSelected);
        AvaloniaPluginTab? preferred = visible.FirstOrDefault(t => !IsDeferredDefault(t));
        if (selected != null && (preferred == null || !_defaultedDeferredTabs.Contains(selected)))
        {
            // The tab control may have moved its own selection while the list changed; put it back.
            TabSelected?.Invoke(selected);
            return;
        }

        AvaloniaPluginTab toSelect = preferred ?? visible[0];
        toSelect.IsSelected = true;
        if (IsDeferredDefault(toSelect))
        {
            _defaultedDeferredTabs.Add(toSelect);
        }
    }

    private static bool IsDeferredDefault(AvaloniaPluginTab tab) => Array.IndexOf(DeferredDefaultTabs, tab.Title) >= 0;
}
