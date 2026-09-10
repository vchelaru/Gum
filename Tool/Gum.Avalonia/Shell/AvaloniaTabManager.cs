using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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

    /// <summary>Creates the manager and restores the saved column widths.</summary>
    public AvaloniaTabManager(IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings)
    {
        _layoutSettings = layoutSettings;
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
        AvaloniaPluginTab tab = new AvaloniaPluginTab(element)
        {
            Title = tabTitle,
            Location = tabLocation,
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

    /// <summary>Marks <paramref name="tab"/> selected and every other tab in its location unselected.</summary>
    public void Select(AvaloniaPluginTab tab)
    {
        foreach (AvaloniaPluginTab other in _tabs.Where(t => t.Location == tab.Location && t != tab))
        {
            other.IsSelected = false;
        }
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
            Refilter();
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

    private void Sync(ObservableCollection<AvaloniaPluginTab> target, TabLocation location)
    {
        AvaloniaPluginTab[] wanted = _tabs.Where(t => TabDockingLogic.ShouldAppearInLocation(t, location)).ToArray();
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
