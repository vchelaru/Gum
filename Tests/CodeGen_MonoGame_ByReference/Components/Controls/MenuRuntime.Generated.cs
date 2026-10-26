//Code for Controls/Menu (Container)
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
[global::Gum.DataTypes.GumElement("Controls/Menu")]
partial class MenuRuntime : ContainerRuntime
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Menu", typeof(MenuRuntime));
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Menu)] = new global::Gum.Forms.VisualTemplate((vm, createForms) => new MenuRuntime(fullInstantiation: true, tryCreateFormsObject: createForms));
    }
    public global::Gum.Forms.Controls.Menu FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.Menu;
    private bool _tryCreateFormsObject = true;
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }

    public MenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        _tryCreateFormsObject = tryCreateFormsObject;
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/Menu");
            element.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null && _tryCreateFormsObject)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.Menu(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as global::Gum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
