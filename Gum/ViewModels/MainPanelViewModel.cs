using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms.Integration;
using Gum.Controls;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.ViewModels;
using Control = System.Windows.Forms.Control;

namespace Gum.Controls;

public class MainPanelViewModel : ViewModel, ITabManager
{
    public IReadOnlyList<PluginTabContainerViewModel> TabContainers { get; }
    
    public PluginTabContainerViewModel CenterBottomContainer { get; }
    public PluginTabContainerViewModel RightBottomContainer { get; }
    public PluginTabContainerViewModel RightTopContainer { get; }
    public PluginTabContainerViewModel CenterTopContainer { get; }
    public PluginTabContainerViewModel LeftContainer { get; }
    public PluginTabContainerViewModel CenterContainer { get; }

    public bool IsToolsVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public MainPanelViewModel(Func<TabLocation, PluginTabContainerViewModel> tabContainerFactory)
    {
        IsToolsVisible = true;
        
        TabContainers =
        [
            CenterBottomContainer = tabContainerFactory(TabLocation.CenterBottom),
            RightBottomContainer = tabContainerFactory(TabLocation.RightBottom),
            RightTopContainer = tabContainerFactory(TabLocation.RightTop),
            CenterTopContainer = tabContainerFactory(TabLocation.CenterTop),
            LeftContainer = tabContainerFactory(TabLocation.Left),
            CenterContainer = tabContainerFactory(TabLocation.Center)
        ];
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

        PluginTab newPluginTab = new(element)
        {
            Title = tabTitle,
            SuggestedLocation = tabLocation
        };
        
        TabContainers.First(c => c.Location == tabLocation).Tabs.Add(newPluginTab);
        return newPluginTab;
    }

    public void RemoveControl(FrameworkElement element)
    {
        if (TabContainers.SelectMany(c => c.Tabs).FirstOrDefault(t => t.Content == element) is { } tab)
        {
            TabContainers.First(c => c.Tabs.Contains(tab)).Tabs.Remove(tab);
        }
    }

    public bool ShowTabForControl(System.Windows.Controls.UserControl control)
    {
        if (TabContainers.SelectMany(c => c.Tabs)
                .FirstOrDefault(t => t.Content == control) is not { } tab)
        {
            return false;
        }
        
        tab.Show();
        return true;
    }

    public bool ShowTabForControl(Control control)
    {
        if (TabContainers.SelectMany(c => c.Tabs)
                .FirstOrDefault(t => t.Content is WindowsFormsHost host && host.Child == control) is not { } tab)
        {
            return false;
        }
        
        tab.Show();
        return true;
    }
}
