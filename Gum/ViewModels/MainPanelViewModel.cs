using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms.Integration;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Services;
using Gum.Settings;
using Microsoft.Extensions.Options;
using Control = System.Windows.Forms.Control;

namespace Gum.Controls;

public class MainPanelViewModel : ViewModel, ITabManager, IRecipient<ApplicationTeardownMessage>
{
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;

    public double LeftColumnWidth
    {
        get => Get<double>();
        set => Set(value);
    }

    public double CenterColumnWidth
    {
        get => Get<double>();
        set => Set(value);
    }
    public double BottomRightHeight
    {
        get => Get<double>();
        set => Set(value);
    }

    private readonly Func<FrameworkElement, PluginTab> _pluginTabFactory;
    private ObservableCollection<PluginTab> PluginTabs { get; } = [];

    public ICollectionView CenterView { get; }
    public ICollectionView RightBottomView { get; }
    public ICollectionView RightTopView { get; }
    public ICollectionView CenterTopView { get; }
    public ICollectionView CenterBottomView { get; }
    public ICollectionView LeftView { get; }
    
    public bool IsToolsVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public MainPanelViewModel(Func<FrameworkElement, PluginTab> pluginTabFactory, IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings)
    {
        _layoutSettings = layoutSettings;
        _pluginTabFactory = pluginTabFactory;
        messenger.RegisterAll(this);

        LeftColumnWidth = layoutSettings.CurrentValue.MainTabDimensions.LeftColumnWidth;
        CenterColumnWidth = layoutSettings.CurrentValue.MainTabDimensions.CenterColumnWidth;
        BottomRightHeight = layoutSettings.CurrentValue.MainTabDimensions.BottomRightHeight;

        IsToolsVisible = true;
        PluginTabs.CollectionChanged += PluginTabsOnCollectionChanged;
        
        CenterView = CreateView(TabLocation.Center);
        RightBottomView = CreateView(TabLocation.RightBottom);
        RightTopView = CreateView(TabLocation.RightTop);
        CenterTopView = CreateView(TabLocation.CenterTop);
        CenterBottomView = CreateView(TabLocation.CenterBottom);
        LeftView = CreateView(TabLocation.Left);
        
        ICollectionView CreateView(TabLocation location)
        {
            ListCollectionView view = new(PluginTabs);
            view.IsLiveFiltering = true;
            view.LiveFilteringProperties.Add(nameof(PluginTab.IsVisible));
            view.LiveFilteringProperties.Add(nameof(PluginTab.Location));

            view.Filter = o => o is PluginTab tab && tab.Location == location && tab.IsVisible;
        
            return view;
        }
    }

    private void PluginTabsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?.OfType<PluginTab>() is { } newTabs)
        {
            foreach (PluginTab newTab in newTabs)
            {
                if (!PluginTabs.Any(p => p.Location == newTab.Location && p.IsSelected))
                {
                    newTab.IsSelected = true;
                }
            }
        }
    }

    public PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle,
        TabLocation tabLocation) =>
        AddControl(new System.Windows.Forms.Integration.WindowsFormsHost() { Child = control }, tabTitle, tabLocation);

    public PluginTab AddControl(FrameworkElement element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
    {
        PluginTab newPluginTab = _pluginTabFactory(element);
        newPluginTab.Title = tabTitle;
        newPluginTab.Location = tabLocation;

        if (element is WindowsFormsHost host && tabTitle == "Editor")
        {   // this is kind of a hack to deal with the the airspace issue
            // blocking mouse interaction with the grid splitter and window resize handle
            host.Margin = new Thickness(4, 0, 4, 0);
        }

        PluginTabs.Add(newPluginTab);
        return newPluginTab;
    }
    
    public void RemoveTab(PluginTab tab) => PluginTabs.Remove(tab);

    private void SaveLayoutSettings()
    {
        _layoutSettings.Update(l =>
        {
            l.MainTabDimensions =
                new MainTabDimensions
                {
                    LeftColumnWidth = LeftColumnWidth,
                    CenterColumnWidth = CenterColumnWidth,
                    BottomRightHeight = BottomRightHeight
                };
        });
    }

    void IRecipient<ApplicationTeardownMessage>.Receive(ApplicationTeardownMessage message)
    {
        message.OnTearDown(SaveLayoutSettings);
    }

    internal void EnsureMinimumWidth()
    {
        const int minWidth = 20;
        LeftColumnWidth = Math.Max(LeftColumnWidth, minWidth);
        CenterColumnWidth = Math.Max(CenterColumnWidth, minWidth);
        BottomRightHeight = Math.Max(BottomRightHeight, minWidth);
    }
}