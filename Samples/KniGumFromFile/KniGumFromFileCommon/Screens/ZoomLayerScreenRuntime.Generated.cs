//Code for ZoomLayerScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class ZoomLayerScreenRuntime:Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ZoomLayerScreen", typeof(ZoomLayerScreenRuntime));
    }
    public ColoredRectangleRuntime Unlayered { get; protected set; }
    public ColoredRectangleRuntime Layered { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public CircleRuntime CircleInstance { get; protected set; }
    public CircleRuntime CircleInstance1 { get; protected set; }
    public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
    public ColoredRectangleRuntime ColoredRectangleInstance1 { get; protected set; }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public NineSliceRuntime NineSliceInstance1 { get; protected set; }
    public PolygonRuntime PolygonInstance { get; protected set; }
    public PolygonRuntime PolygonInstance1 { get; protected set; }
    public SpriteRuntime SpriteInstance { get; protected set; }
    public SpriteRuntime SpriteInstance1 { get; protected set; }
    public ScreenChangingComponentRuntime ScreenChangingComponentInstance { get; protected set; }

    public ZoomLayerScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("ZoomLayerScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Unlayered = this.GetGraphicalUiElementByName("Unlayered") as ColoredRectangleRuntime;
        Layered = this.GetGraphicalUiElementByName("Layered") as ColoredRectangleRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        TextInstance1 = this.GetGraphicalUiElementByName("TextInstance1") as TextRuntime;
        CircleInstance = this.GetGraphicalUiElementByName("CircleInstance") as CircleRuntime;
        CircleInstance1 = this.GetGraphicalUiElementByName("CircleInstance1") as CircleRuntime;
        ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
        ColoredRectangleInstance1 = this.GetGraphicalUiElementByName("ColoredRectangleInstance1") as ColoredRectangleRuntime;
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        NineSliceInstance1 = this.GetGraphicalUiElementByName("NineSliceInstance1") as NineSliceRuntime;
        PolygonInstance = this.GetGraphicalUiElementByName("PolygonInstance") as PolygonRuntime;
        PolygonInstance1 = this.GetGraphicalUiElementByName("PolygonInstance1") as PolygonRuntime;
        SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
        SpriteInstance1 = this.GetGraphicalUiElementByName("SpriteInstance1") as SpriteRuntime;
        ScreenChangingComponentInstance = this.GetGraphicalUiElementByName("ScreenChangingComponentInstance") as ScreenChangingComponentRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
