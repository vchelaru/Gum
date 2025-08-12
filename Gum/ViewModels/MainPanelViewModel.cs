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
using Control = System.Windows.Forms.Control;

namespace Gum.Controls;

public class MainPanelViewModel : ViewModel, ITabManager
{
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

    public MainPanelViewModel(Func<FrameworkElement, PluginTab> pluginTabFactory)
    {
        _pluginTabFactory = pluginTabFactory;
        
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
        // This should be moved to the MainPanelControl wpf 
        string AppTheme = "Light";
        element.Resources = new System.Windows.ResourceDictionary();
        element.Resources.Source = new Uri($"/Themes/{AppTheme}.xaml", UriKind.Relative);

        PluginTab newPluginTab = _pluginTabFactory(element);
        newPluginTab.Title = tabTitle;
        newPluginTab.Location = tabLocation;
        
        PluginTabs.Add(newPluginTab);
        return newPluginTab;
    }
}