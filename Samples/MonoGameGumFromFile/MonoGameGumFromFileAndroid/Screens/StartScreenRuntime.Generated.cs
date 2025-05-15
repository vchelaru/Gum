//Code for StartScreen
using GumRuntime;
using MonoGameGumFromFileAndroid.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumFromFileAndroid.Screens
{
    public partial class StartScreenRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StartScreen", typeof(StartScreenRuntime));
        }
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime TextInstance1 { get; protected set; }
        public TextRuntime TextInstance2 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
        public CircleRuntime CircleInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public TextRuntime TextInstance3 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance1 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance2 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance3 { get; protected set; }
        public ColoredRectangleRuntime ColoredRectangleInstance4 { get; protected set; }
        public TextRuntime TextInstance7 { get; protected set; }
        public SpriteRuntime SpriteInstance1 { get; protected set; }
        public SpriteRuntime SpriteInstance2 { get; protected set; }
        public TextRuntime TextInstance8 { get; protected set; }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public TextRuntime TextInstance4 { get; protected set; }
        public PolygonRuntime PolygonInstance { get; protected set; }
        public TextRuntime TextInstance5 { get; protected set; }
        public RectangleRuntime RectangleInstance { get; protected set; }
        public TextRuntime TextInstance6 { get; protected set; }
        public SpriteRuntime SpriteInstance { get; protected set; }
        public ContainerRuntime ContainerInstance1 { get; protected set; }
        public TextRuntime TextInstance9 { get; protected set; }
        public ComponentWithExposedVariableRuntime ComponentWithExposedVariableInstance { get; protected set; }

        public StartScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("StartScreen");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            TextInstance1 = this.GetGraphicalUiElementByName("TextInstance1") as TextRuntime;
            TextInstance2 = this.GetGraphicalUiElementByName("TextInstance2") as TextRuntime;
            ColoredRectangleInstance = this.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
            CircleInstance = this.GetGraphicalUiElementByName("CircleInstance") as CircleRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            TextInstance3 = this.GetGraphicalUiElementByName("TextInstance3") as TextRuntime;
            ColoredRectangleInstance1 = this.GetGraphicalUiElementByName("ColoredRectangleInstance1") as ColoredRectangleRuntime;
            ColoredRectangleInstance2 = this.GetGraphicalUiElementByName("ColoredRectangleInstance2") as ColoredRectangleRuntime;
            ColoredRectangleInstance3 = this.GetGraphicalUiElementByName("ColoredRectangleInstance3") as ColoredRectangleRuntime;
            ColoredRectangleInstance4 = this.GetGraphicalUiElementByName("ColoredRectangleInstance4") as ColoredRectangleRuntime;
            TextInstance7 = this.GetGraphicalUiElementByName("TextInstance7") as TextRuntime;
            SpriteInstance1 = this.GetGraphicalUiElementByName("SpriteInstance1") as SpriteRuntime;
            SpriteInstance2 = this.GetGraphicalUiElementByName("SpriteInstance2") as SpriteRuntime;
            TextInstance8 = this.GetGraphicalUiElementByName("TextInstance8") as TextRuntime;
            NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
            TextInstance4 = this.GetGraphicalUiElementByName("TextInstance4") as TextRuntime;
            PolygonInstance = this.GetGraphicalUiElementByName("PolygonInstance") as PolygonRuntime;
            TextInstance5 = this.GetGraphicalUiElementByName("TextInstance5") as TextRuntime;
            RectangleInstance = this.GetGraphicalUiElementByName("RectangleInstance") as RectangleRuntime;
            TextInstance6 = this.GetGraphicalUiElementByName("TextInstance6") as TextRuntime;
            SpriteInstance = this.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as ContainerRuntime;
            TextInstance9 = this.GetGraphicalUiElementByName("TextInstance9") as TextRuntime;
            ComponentWithExposedVariableInstance = this.GetGraphicalUiElementByName("ComponentWithExposedVariableInstance") as ComponentWithExposedVariableRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
