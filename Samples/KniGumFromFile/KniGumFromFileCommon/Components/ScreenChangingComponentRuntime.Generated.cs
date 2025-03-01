//Code for ScreenChangingComponent (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class ScreenChangingComponentRuntime:ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ScreenChangingComponent", typeof(ScreenChangingComponentRuntime));
    }
    public ButtonStandardRuntime ButtonScreen1 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen2 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen3 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen4 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen5 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen6 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen7 { get; protected set; }
    public ButtonStandardRuntime ButtonScreen8 { get; protected set; }

    public ScreenChangingComponentRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("ScreenChangingComponent");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        ButtonScreen1 = this.GetGraphicalUiElementByName("ButtonScreen1") as ButtonStandardRuntime;
        ButtonScreen2 = this.GetGraphicalUiElementByName("ButtonScreen2") as ButtonStandardRuntime;
        ButtonScreen3 = this.GetGraphicalUiElementByName("ButtonScreen3") as ButtonStandardRuntime;
        ButtonScreen4 = this.GetGraphicalUiElementByName("ButtonScreen4") as ButtonStandardRuntime;
        ButtonScreen5 = this.GetGraphicalUiElementByName("ButtonScreen5") as ButtonStandardRuntime;
        ButtonScreen6 = this.GetGraphicalUiElementByName("ButtonScreen6") as ButtonStandardRuntime;
        ButtonScreen7 = this.GetGraphicalUiElementByName("ButtonScreen7") as ButtonStandardRuntime;
        ButtonScreen8 = this.GetGraphicalUiElementByName("ButtonScreen8") as ButtonStandardRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
