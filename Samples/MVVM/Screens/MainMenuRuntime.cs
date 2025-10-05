using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameAndGum.ViewModels;
using System;
using MonoGameAndGum.DataDefinitions;
using System.ComponentModel;
partial class MainMenuRuntime : Gum.Wireframe.BindableGue
{
    MainMenuViewModel ViewModel;

    partial void CustomInitialize()
    {
        ViewModel = new MainMenuViewModel();

        // Populate your ViewModel according to your Model
        ViewModel.Money = 5000;

        var inventory = new StoreInventory();

        foreach(var item in inventory.Items)
        {
            var itemViewModel = new ShopItemViewModel();
            itemViewModel.SetFrom(item);
            ViewModel.ShopItems.Add(itemViewModel);
        }

        this.BindingContext = ViewModel;

        BuyButton.FormsControl.Click += (_, _) =>
        {
            ViewModel.HandleBuy();
        };
        AddMoneyButton.FormsControl.Click += (_, _) => 
            ViewModel.AddMoney();

        MoneyDisplay.SetBinding(
            //"Text",
            nameof(MoneyDisplay.LabelText),
            nameof(ViewModel.MoneyDisplay));

        BuyButton.SetBinding(
            nameof(BuyButton.IsEnabled),
            nameof(ViewModel.HasEnoughMoney));

        NotEnoughMoney.SetBinding(
            nameof(NotEnoughMoney.Visible),
            nameof(ViewModel.IsNeedMoreMoneyVisible));


        var listBox = ListBoxInstance.FormsControl;
        listBox.SetBinding(
            nameof(listBox.Items),
            nameof(ViewModel.ShopItems));

        listBox.SetBinding(
            nameof(listBox.SelectedObject),
            nameof(ViewModel.SelectedItem));

        listBox.VisualTemplate = new Gum.Forms.VisualTemplate(
            () => new StoreListBoxItemRuntime(fullInstantiation:true, tryCreateFormsObject:false));

        InventoryLabel.SetBinding(
            nameof(InventoryLabel.LabelText),
            nameof(ViewModel.TotalValueDisplay));

        ViewModel.PropertyChanged += HandlePropertyChanged;

    }

    private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case nameof(ViewModel.Money):
                
                break;
        }
    }
}
