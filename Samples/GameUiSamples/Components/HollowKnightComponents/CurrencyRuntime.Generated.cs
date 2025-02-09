//Code for HollowKnightComponents/Currency (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using GameUiSamples.Components;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components
{
    public partial class CurrencyRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("HollowKnightComponents/Currency", typeof(CurrencyRuntime));
        }
        public SpriteRuntime SpriteInstance { get; protected set; }
        public TextRuntime TotalMoneyTextInstance { get; protected set; }
        public TextRuntime ToAddTextInstance { get; protected set; }

        public CurrencyRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            TotalMoneyTextInstance = this.GetGraphicalUiElementByName("TotalMoneyTextInstance") as TextRuntime;
            ToAddTextInstance = this.GetGraphicalUiElementByName("ToAddTextInstance") as TextRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
