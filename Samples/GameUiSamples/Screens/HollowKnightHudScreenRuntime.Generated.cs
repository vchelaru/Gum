//Code for HollowKnightHudScreen
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Screens
{
    public partial class HollowKnightHudScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightHudScreen", typeof(HollowKnightHudScreenRuntime));
        }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
        public ContainerRuntime MainHudContainer { get; protected set; }
        public ManaOrbRuntime ManaOrbInstance { get; protected set; }
        public ContainerRuntime HealthContainer { get; protected set; }
        public HealthItemRuntime HealthItemInstance { get; protected set; }
        public HealthItemRuntime HealthItemInstance1 { get; protected set; }
        public HealthItemRuntime HealthItemInstance2 { get; protected set; }
        public HealthItemRuntime HealthItemInstance3 { get; protected set; }
        public HealthItemRuntime HealthItemInstance4 { get; protected set; }
        public HealthItemRuntime HealthItemInstance5 { get; protected set; }
        public CurrencyRuntime CurrencyInstance { get; protected set; }
        public ContainerRuntime ActionButtonContainer { get; protected set; }
        public ButtonStandardRuntime AddManaButton { get; protected set; }
        public ButtonStandardRuntime SubtractManaButton { get; protected set; }
        public ButtonStandardRuntime TakeDamageButton { get; protected set; }
        public ButtonStandardRuntime RefillHealthButton { get; protected set; }
        public ButtonStandardRuntime AddMoneyButton { get; protected set; }

        public HollowKnightHudScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            MainHudContainer = this.GetGraphicalUiElementByName("MainHudContainer") as ContainerRuntime;
            ManaOrbInstance = this.GetGraphicalUiElementByName("ManaOrbInstance") as ManaOrbRuntime;
            HealthContainer = this.GetGraphicalUiElementByName("HealthContainer") as ContainerRuntime;
            HealthItemInstance = this.GetGraphicalUiElementByName("HealthItemInstance") as HealthItemRuntime;
            HealthItemInstance1 = this.GetGraphicalUiElementByName("HealthItemInstance1") as HealthItemRuntime;
            HealthItemInstance2 = this.GetGraphicalUiElementByName("HealthItemInstance2") as HealthItemRuntime;
            HealthItemInstance3 = this.GetGraphicalUiElementByName("HealthItemInstance3") as HealthItemRuntime;
            HealthItemInstance4 = this.GetGraphicalUiElementByName("HealthItemInstance4") as HealthItemRuntime;
            HealthItemInstance5 = this.GetGraphicalUiElementByName("HealthItemInstance5") as HealthItemRuntime;
            CurrencyInstance = this.GetGraphicalUiElementByName("CurrencyInstance") as CurrencyRuntime;
            ActionButtonContainer = this.GetGraphicalUiElementByName("ActionButtonContainer") as ContainerRuntime;
            AddManaButton = this.GetGraphicalUiElementByName("AddManaButton") as ButtonStandardRuntime;
            SubtractManaButton = this.GetGraphicalUiElementByName("SubtractManaButton") as ButtonStandardRuntime;
            TakeDamageButton = this.GetGraphicalUiElementByName("TakeDamageButton") as ButtonStandardRuntime;
            RefillHealthButton = this.GetGraphicalUiElementByName("RefillHealthButton") as ButtonStandardRuntime;
            AddMoneyButton = this.GetGraphicalUiElementByName("AddMoneyButton") as ButtonStandardRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
