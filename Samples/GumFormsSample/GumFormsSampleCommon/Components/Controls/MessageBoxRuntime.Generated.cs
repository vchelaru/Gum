//Code for Controls/MessageBox (Controls/UserControl)
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class MessageBoxRuntime:UserControlRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MessageBox", typeof(MessageBoxRuntime));
        }
        public LabelRuntime LabelInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public ButtonConfirmRuntime OkButton { get; protected set; }
        public ButtonDenyRuntime CancelButton { get; protected set; }

        public MessageBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/MessageBox");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            base.AfterFullCreation();
            LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as LabelRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            OkButton = this.GetGraphicalUiElementByName("OkButton") as ButtonConfirmRuntime;
            CancelButton = this.GetGraphicalUiElementByName("CancelButton") as ButtonDenyRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
