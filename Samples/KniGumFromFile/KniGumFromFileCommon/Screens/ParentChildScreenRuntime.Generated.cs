//Code for ParentChildScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class ParentChildScreenRuntime:Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ParentChildScreen", typeof(ParentChildScreenRuntime));
    }
    public ColoredRectangleRuntime OuterRectangle { get; protected set; }
    public ColoredRectangleRuntime TopLeft { get; protected set; }
    public ScreenChangingComponentRuntime ScreenChangingComponentInstance { get; protected set; }

    public ParentChildScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("ParentChildScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        OuterRectangle = this.GetGraphicalUiElementByName("OuterRectangle") as ColoredRectangleRuntime;
        TopLeft = this.GetGraphicalUiElementByName("TopLeft") as ColoredRectangleRuntime;
        ScreenChangingComponentInstance = this.GetGraphicalUiElementByName("ScreenChangingComponentInstance") as ScreenChangingComponentRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
