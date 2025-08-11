using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Gum.Mvvm;
using Gum.Plugins;

namespace Gum.ViewModels;

public class PluginTabContainerViewModel : ViewModel
{
    public ObservableCollection<PluginTab> Tabs { get; } = [];
    
    public TabLocation Location { get; }
    
    public PluginTab? SelectedTab
    {
        get => Get<PluginTab>();
        set
        {
            if (Set(value) && value != null)
            {
                SelectedTabIndex = Tabs.IndexOf(value);
            }
        }
    }

    public int SelectedTabIndex
    {
        get => Get<int>();
        set => Set(value);
    }

    public PluginTabContainerViewModel(TabLocation tabLocation)
    {
        Location = tabLocation;
        Tabs.CollectionChanged += TabsOnCollectionChanged;
    }

    private void TabsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (PluginTab addedTab in e.NewItems?.OfType<PluginTab>() ?? [])
        {
            addedTab.ParentContainer = this;
            addedTab.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PluginTab.IsVisible))
                {
                    if (Tabs.FirstOrDefault(t => t.IsVisible) is { } visibleTab)
                    {
                        SelectedTab = visibleTab;
                    }
                    else
                    {
                        SelectedTab = null;
                    }
                }
            };
        }
    }
}
