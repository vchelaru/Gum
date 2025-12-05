//Code for MainMenu
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
partial class MainMenuRuntime : Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("MainMenu", typeof(MainMenuRuntime));
    }
    public ListBoxRuntime ListBoxInstance { get; protected set; }
    public LabelRuntime InventoryLabel { get; protected set; }
    public LabelRuntime NotEnoughMoney { get; protected set; }
    public ButtonStandardRuntime BuyButton { get; protected set; }
    public LabelRuntime MoneyDisplay { get; protected set; }
    public ButtonStandardRuntime AddMoneyButton { get; protected set; }

    public MainMenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("MainMenu");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        ListBoxInstance = this.GetGraphicalUiElementByName("ListBoxInstance") as ListBoxRuntime;
        InventoryLabel = this.GetGraphicalUiElementByName("InventoryLabel") as LabelRuntime;
        NotEnoughMoney = this.GetGraphicalUiElementByName("NotEnoughMoney") as LabelRuntime;
        BuyButton = this.GetGraphicalUiElementByName("BuyButton") as ButtonStandardRuntime;
        MoneyDisplay = this.GetGraphicalUiElementByName("MoneyDisplay") as LabelRuntime;
        AddMoneyButton = this.GetGraphicalUiElementByName("AddMoneyButton") as ButtonStandardRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
