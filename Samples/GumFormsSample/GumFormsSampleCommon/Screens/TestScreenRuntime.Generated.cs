//Code for TestScreen
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class TestScreenRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("TestScreen", typeof(TestScreenRuntime));
    }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public ContainerRuntime ContainerInstance1 { get; protected set; }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public NineSliceRuntime NineSliceInstance12 { get; protected set; }
    public NineSliceRuntime NineSliceInstance1 { get; protected set; }
    public NineSliceRuntime NineSliceInstance13 { get; protected set; }
    public NineSliceRuntime NineSliceInstance2 { get; protected set; }
    public NineSliceRuntime NineSliceInstance14 { get; protected set; }
    public NineSliceRuntime NineSliceInstance3 { get; protected set; }
    public NineSliceRuntime NineSliceInstance15 { get; protected set; }
    public NineSliceRuntime NineSliceInstance4 { get; protected set; }
    public NineSliceRuntime NineSliceInstance16 { get; protected set; }
    public NineSliceRuntime NineSliceInstance5 { get; protected set; }
    public NineSliceRuntime NineSliceInstance17 { get; protected set; }
    public NineSliceRuntime NineSliceInstance6 { get; protected set; }
    public NineSliceRuntime NineSliceInstance18 { get; protected set; }
    public NineSliceRuntime NineSliceInstance7 { get; protected set; }
    public NineSliceRuntime NineSliceInstance19 { get; protected set; }
    public NineSliceRuntime NineSliceInstance8 { get; protected set; }
    public NineSliceRuntime NineSliceInstance20 { get; protected set; }
    public NineSliceRuntime NineSliceInstance9 { get; protected set; }
    public NineSliceRuntime NineSliceInstance21 { get; protected set; }
    public NineSliceRuntime NineSliceInstance10 { get; protected set; }
    public NineSliceRuntime NineSliceInstance22 { get; protected set; }
    public NineSliceRuntime NineSliceInstance23 { get; protected set; }
    public NineSliceRuntime NineSliceInstance24 { get; protected set; }
    public NineSliceRuntime NineSliceInstance25 { get; protected set; }
    public NineSliceRuntime NineSliceInstance26 { get; protected set; }
    public NineSliceRuntime NineSliceInstance27 { get; protected set; }
    public NineSliceRuntime NineSliceInstance28 { get; protected set; }
    public NineSliceRuntime NineSliceInstance11 { get; protected set; }

    public TestScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("TestScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            AfterFullCreation();
        }



    }
    public override void AfterFullCreation()
    {
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
        ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as ContainerRuntime;
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        NineSliceInstance12 = this.GetGraphicalUiElementByName("NineSliceInstance12") as NineSliceRuntime;
        NineSliceInstance1 = this.GetGraphicalUiElementByName("NineSliceInstance1") as NineSliceRuntime;
        NineSliceInstance13 = this.GetGraphicalUiElementByName("NineSliceInstance13") as NineSliceRuntime;
        NineSliceInstance2 = this.GetGraphicalUiElementByName("NineSliceInstance2") as NineSliceRuntime;
        NineSliceInstance14 = this.GetGraphicalUiElementByName("NineSliceInstance14") as NineSliceRuntime;
        NineSliceInstance3 = this.GetGraphicalUiElementByName("NineSliceInstance3") as NineSliceRuntime;
        NineSliceInstance15 = this.GetGraphicalUiElementByName("NineSliceInstance15") as NineSliceRuntime;
        NineSliceInstance4 = this.GetGraphicalUiElementByName("NineSliceInstance4") as NineSliceRuntime;
        NineSliceInstance16 = this.GetGraphicalUiElementByName("NineSliceInstance16") as NineSliceRuntime;
        NineSliceInstance5 = this.GetGraphicalUiElementByName("NineSliceInstance5") as NineSliceRuntime;
        NineSliceInstance17 = this.GetGraphicalUiElementByName("NineSliceInstance17") as NineSliceRuntime;
        NineSliceInstance6 = this.GetGraphicalUiElementByName("NineSliceInstance6") as NineSliceRuntime;
        NineSliceInstance18 = this.GetGraphicalUiElementByName("NineSliceInstance18") as NineSliceRuntime;
        NineSliceInstance7 = this.GetGraphicalUiElementByName("NineSliceInstance7") as NineSliceRuntime;
        NineSliceInstance19 = this.GetGraphicalUiElementByName("NineSliceInstance19") as NineSliceRuntime;
        NineSliceInstance8 = this.GetGraphicalUiElementByName("NineSliceInstance8") as NineSliceRuntime;
        NineSliceInstance20 = this.GetGraphicalUiElementByName("NineSliceInstance20") as NineSliceRuntime;
        NineSliceInstance9 = this.GetGraphicalUiElementByName("NineSliceInstance9") as NineSliceRuntime;
        NineSliceInstance21 = this.GetGraphicalUiElementByName("NineSliceInstance21") as NineSliceRuntime;
        NineSliceInstance10 = this.GetGraphicalUiElementByName("NineSliceInstance10") as NineSliceRuntime;
        NineSliceInstance22 = this.GetGraphicalUiElementByName("NineSliceInstance22") as NineSliceRuntime;
        NineSliceInstance23 = this.GetGraphicalUiElementByName("NineSliceInstance23") as NineSliceRuntime;
        NineSliceInstance24 = this.GetGraphicalUiElementByName("NineSliceInstance24") as NineSliceRuntime;
        NineSliceInstance25 = this.GetGraphicalUiElementByName("NineSliceInstance25") as NineSliceRuntime;
        NineSliceInstance26 = this.GetGraphicalUiElementByName("NineSliceInstance26") as NineSliceRuntime;
        NineSliceInstance27 = this.GetGraphicalUiElementByName("NineSliceInstance27") as NineSliceRuntime;
        NineSliceInstance28 = this.GetGraphicalUiElementByName("NineSliceInstance28") as NineSliceRuntime;
        NineSliceInstance11 = this.GetGraphicalUiElementByName("NineSliceInstance11") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
