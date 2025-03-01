//Code for StateScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class StateScreenRuntime:Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StateScreen", typeof(StateScreenRuntime));
    }
    public ComponentWithStateRuntime ComponentWithStateInstance { get; protected set; }
    public ComponentWithStateRuntime SetMeInCode { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public ScreenChangingComponentRuntime ScreenChangingComponentInstance { get; protected set; }

    public StateScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("StateScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        ComponentWithStateInstance = this.GetGraphicalUiElementByName("ComponentWithStateInstance") as ComponentWithStateRuntime;
        SetMeInCode = this.GetGraphicalUiElementByName("SetMeInCode") as ComponentWithStateRuntime;
        TextInstance1 = this.GetGraphicalUiElementByName("TextInstance1") as TextRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        ScreenChangingComponentInstance = this.GetGraphicalUiElementByName("ScreenChangingComponentInstance") as ScreenChangingComponentRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
