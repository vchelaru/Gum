//Code for Controls/DialogBox (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class DialogBoxRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/DialogBox", typeof(DialogBoxRuntime));
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public IconRuntime ContinueIndicatorInstance { get; protected set; }

    public DialogBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/DialogBox");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        ContinueIndicatorInstance = this.GetGraphicalUiElementByName("ContinueIndicatorInstance") as IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
