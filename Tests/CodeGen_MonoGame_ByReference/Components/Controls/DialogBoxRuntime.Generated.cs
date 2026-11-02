//Code for Controls/DialogBox (Container)
using CodeGen_MonoGame_ByReference.Components.Elements;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGame_ByReference.Components.Controls;
[global::Gum.DataTypes.GumElement("Controls/DialogBox", FormsType = typeof(global::Gum.Forms.Controls.Games.DialogBox))]
partial class DialogBoxRuntime : ContainerRuntime
{
    #if UNITY_5_3_OR_NEWER && !GUM_NO_UNITY_ENGINE
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/DialogBox", typeof(DialogBoxRuntime));
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Games.DialogBox)] = new global::Gum.Forms.VisualTemplate((vm, createForms) => new DialogBoxRuntime(fullInstantiation: true, tryCreateFormsObject: createForms));
    }
    public global::Gum.Forms.Controls.Games.DialogBox FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.Games.DialogBox;
    private bool _tryCreateFormsObject = true;
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public IconRuntime ContinueIndicatorInstance { get; protected set; }

    public DialogBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        _tryCreateFormsObject = tryCreateFormsObject;
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/DialogBox");
            element.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null && _tryCreateFormsObject)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.Games.DialogBox(this);
        }
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as global::Gum.GueDeriving.NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::Gum.GueDeriving.TextRuntime;
        ContinueIndicatorInstance = this.GetGraphicalUiElementByName("ContinueIndicatorInstance") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
