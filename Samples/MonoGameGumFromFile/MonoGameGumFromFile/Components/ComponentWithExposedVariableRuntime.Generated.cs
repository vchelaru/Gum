//Code for ComponentWithExposedVariable (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumFromFile.Components
{
    public partial class ComponentWithExposedVariableRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ComponentWithExposedVariable", typeof(ComponentWithExposedVariableRuntime));
        }
        public TextRuntime TextInstance { get; protected set; }

        public string Text
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public ComponentWithExposedVariableRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {


        }
        public override void AfterFullCreation()
        {
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
