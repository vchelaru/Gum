//Code for ComponentWithExposedVariable (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using SkiaGum;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;

namespace MonoGameGumFromFileAndroid.Components
{
    public partial class ComponentWithExposedVariableRuntime:ContainerRuntime
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
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("ComponentWithExposedVariable");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



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
