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
        public ContainerRuntime ContainerInstance1 { get; protected set; }
        public ContainerRuntime ContainerInstance2 { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance1 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance3 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance2 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance4 { get; protected set; }

        public TestScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as ContainerRuntime;
            ContainerInstance2 = this.GetGraphicalUiElementByName("ContainerInstance2") as ContainerRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            ColoredRectangleInstance1 = this.GetGraphicalUiElementByName("ColoredRectangleInstance1") as ColoredRectangleRuntime;
            ColoredRectangleInstance3 = this.GetGraphicalUiElementByName("ColoredRectangleInstance3") as ColoredRectangleRuntime;
            ColoredRectangleInstance2 = this.GetGraphicalUiElementByName("ColoredRectangleInstance2") as ColoredRectangleRuntime;
            ColoredRectangleInstance4 = this.GetGraphicalUiElementByName("ColoredRectangleInstance4") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
