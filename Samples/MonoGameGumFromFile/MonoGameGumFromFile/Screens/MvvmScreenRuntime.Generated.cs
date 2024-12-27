//Code for MvvmScreen
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumFromFile.Screens
{
    public partial class MvvmScreenRuntime
    {
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("MvvmScreen", typeof(MvvmScreenRuntime));
        }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

        public MvvmScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
