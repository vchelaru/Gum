//Code for TextScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class TextScreenRuntime:Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("TextScreen", typeof(TextScreenRuntime));
    }
    public TextRuntime NormalText { get; protected set; }
    public TextRuntime NormalText3 { get; protected set; }
    public TextRuntime NormalText2 { get; protected set; }
    public TextRuntime DifferentFontAndSize { get; protected set; }
    public TextRuntime ToggleFontSizes { get; protected set; }
    public TextRuntime NormalText1 { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public ScreenChangingComponentRuntime ScreenChangingComponentInstance { get; protected set; }

    public TextScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("TextScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        NormalText = this.GetGraphicalUiElementByName("NormalText") as TextRuntime;
        NormalText3 = this.GetGraphicalUiElementByName("NormalText3") as TextRuntime;
        NormalText2 = this.GetGraphicalUiElementByName("NormalText2") as TextRuntime;
        DifferentFontAndSize = this.GetGraphicalUiElementByName("DifferentFontAndSize") as TextRuntime;
        ToggleFontSizes = this.GetGraphicalUiElementByName("ToggleFontSizes") as TextRuntime;
        NormalText1 = this.GetGraphicalUiElementByName("NormalText1") as TextRuntime;
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
        ScreenChangingComponentInstance = this.GetGraphicalUiElementByName("ScreenChangingComponentInstance") as ScreenChangingComponentRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
