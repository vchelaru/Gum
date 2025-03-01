//Code for OffsetLayerScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class OffsetLayerScreenRuntime:Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("OffsetLayerScreen", typeof(OffsetLayerScreenRuntime));
    }
    public TextRuntime UnlayeredText { get; protected set; }
    public TextRuntime LayeredText { get; protected set; }
    public ScreenChangingComponentRuntime ScreenChangingComponentInstance { get; protected set; }

    public OffsetLayerScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("OffsetLayerScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        UnlayeredText = this.GetGraphicalUiElementByName("UnlayeredText") as TextRuntime;
        LayeredText = this.GetGraphicalUiElementByName("LayeredText") as TextRuntime;
        ScreenChangingComponentInstance = this.GetGraphicalUiElementByName("ScreenChangingComponentInstance") as ScreenChangingComponentRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
