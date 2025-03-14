//Code for Controls/Menu (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class MenuRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Menu", typeof(MenuRuntime));
        }
        public MonoGameGum.Forms.Controls.Menu FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Menu;
        public NineSliceRuntime Background { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }

        public MenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/Menu");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            if (FormsControl == null)
            {
                FormsControlAsObject = new MonoGameGum.Forms.Controls.Menu(this);
            }
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as ContainerRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
