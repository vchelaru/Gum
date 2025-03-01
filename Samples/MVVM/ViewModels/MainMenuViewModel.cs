using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public ShopItemViewModel SelectedItem
    {
        get => Get<ShopItemViewModel>();
        set => Set(value);
    }

    public MainMenuViewModel()
    {
        ShopItems = new ObservableCollection<ShopItemViewModel>();
    }

    internal void AddMoney()
    {
        Money += 1000;
    }

    internal void HandleBuy()
    {
        Money -= SelectedItem.Cost;
    }
}
