//Code for TestScreen
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
    public partial class TestScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("TestScreen", typeof(TestScreenRuntime));
        }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public TestScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
