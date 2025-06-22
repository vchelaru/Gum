using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.ViewModels;
internal class MainMenuViewModel : ViewModel
{
    public int Money
    //{ get; set; }
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(Money))]
    public string MoneyDisplay =>
        $"Money: ${Money:N0}";

    [DependsOn(nameof(Money))]
    [DependsOn(nameof(SelectedItem))]
    public bool HasEnoughMoney => SelectedItem != null && Money >= SelectedItem.Cost;


    [DependsOn(nameof(HasEnoughMoney))]
    public bool IsNeedMoreMoneyVisible => !HasEnoughMoney;

    public ObservableCollection<ShopItemViewModel> ShopItems
    {
        get => Get<ObservableCollection<ShopItemViewModel>>();
        set => Set(value);
    }

    public int TotalValue
    {
        get => Get<int>();
        private set => Set(value);
    }

    [DependsOn(nameof(TotalValue))]
    public string TotalValueDisplay => $"Total Value: ${TotalValue:N0}";

    public ObservableCollection<InventoryViewModel> Inventory
    {
        get => Get<ObservableCollection<InventoryViewModel>>();
        set => Set(value);
    }

    public ShopItemViewModel SelectedItem
    {
        get => Get<ShopItemViewModel>();
        set => Set(value);
    }

    public MainMenuViewModel()
    {
        ShopItems = new ObservableCollection<ShopItemViewModel>();
        Inventory = new ObservableCollection<InventoryViewModel>();
        Inventory.CollectionChanged += HandleInventoryCollectionChanged;
    }

    private void HandleInventoryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (InventoryViewModel item in e.NewItems)
            {
                item.PropertyChanged += HandleInventoryItemPropertyChanged;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (InventoryViewModel item in e.OldItems)
            {
                item.PropertyChanged -= HandleInventoryItemPropertyChanged;
            }
        }
        UpdateTotalValue();
    }

    private void HandleInventoryItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        UpdateTotalValue();
    }

    private void UpdateTotalValue()
    {
        TotalValue = Inventory.Sum(item => item.Count * item.ShopItem.Cost);
    }

    internal void AddMoney()
    {
        Money += 1000;
    }

    internal void HandleBuy()
    {
        Money -= SelectedItem.Cost;

        var inventoryItem = Inventory.FirstOrDefault(item => item.ShopItem.Name == SelectedItem.Name);
        if (inventoryItem != null)
        {
            inventoryItem.Count++;
        }
        else
        {
            var newInventoryItem = new InventoryViewModel
            {
                ShopItem = SelectedItem.ShopItem,
                Count = 1
            };
            Inventory.Add(newInventoryItem);
        }
    }
}
